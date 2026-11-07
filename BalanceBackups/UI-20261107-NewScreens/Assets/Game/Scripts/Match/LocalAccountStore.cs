using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Bellator.Match
{
    public sealed class LocalPlayerProfile
    {
        public string Id { get; }
        public string Username { get; }
        public string CreatedAtUtc { get; }
        internal LocalPlayerProfile(string id,string username,string created)
        { Id=id; Username=username; CreatedAtUtc=created; }
    }
    public sealed class AccountResult
    {
        public bool Success => Profile!=null;
        public LocalPlayerProfile Profile { get; }
        public string Error { get; }
        internal AccountResult(LocalPlayerProfile profile,string error=null) { Profile=profile; Error=error; }
    }

    /// <summary>Offline credentials and public profile data live in separate files, committed as one directory.</summary>
    public sealed class LocalAccountStore
    {
        public const int Iterations=600000;
        public const string InvalidCredentials="Nome de usuário ou senha incorretos.";
        const int SaltBytes=16,KeyBytes=32;
        [Serializable] sealed class ProfileFile { public int version; public string id,username,createdAtUtc; }
        sealed class Record { public LocalPlayerProfile profile; public string key; public int iterations; public byte[] salt,verifier; }
        readonly string root;
        readonly Dictionary<string,Record> records=new Dictionary<string,Record>(StringComparer.Ordinal);
        bool busy,damaged;
        public string LastError { get; private set; }
        public string RootPath => root;
        public LocalAccountStore(string directory=null)
        { root=Path.GetFullPath(directory??Path.Combine(Application.persistentDataPath,"BellatorAccounts")); Reload(); }

        public static bool TryUsername(string input,out string display,out string key)
        {
            display=key=null;
            try { display=(input??"").Trim().Normalize(NormalizationForm.FormKC); }
            catch(ArgumentException) { return false; }
            if(display.Length==0 || display.Length>24) return false;
            foreach(char c in display) if(!char.IsLetterOrDigit(c) && c!='_' && c!='-' && c!='.' && c!=' ') return false;
            key=display.ToUpperInvariant(); return true;
        }
        public void Reload()
        {
            records.Clear(); damaged=false; LastError=null;
            try
            {
                if(!Directory.Exists(root)) return;
                foreach(string directory in Directory.GetDirectories(root))
                {
                    string id=Path.GetFileName(directory);
                    if(!Guid.TryParseExact(id,"N",out _)) continue; // Incomplete staging directories were never committed.
                    try
                    {
                        var profile=ReadProfile(directory,id);
                        var record=ReadCredential(directory,profile);
                        if(records.ContainsKey(record.key)) throw new InvalidDataException("Perfil duplicado.");
                        records.Add(record.key,record);
                    }
                    catch(Exception) { damaged=true; LastError="Um perfil local está inválido ou indisponível. As contas válidas e o modo convidado continuam disponíveis."; }
                }
            }
            catch(Exception) { damaged=true; LastError="Não foi possível ler os perfis locais. Você pode jogar como convidado."; }
        }
        LocalPlayerProfile ReadProfile(string directory,string id)
        {
            Exception failure=null;
            foreach(string suffix in new[]{"",".bak"})
            {
                try
                {
                    string path=Path.Combine(directory,"profile.json"+suffix);
                    if(!File.Exists(path) || new FileInfo(path).Length>4096) throw new InvalidDataException();
                    var data=JsonUtility.FromJson<ProfileFile>(File.ReadAllText(path));
                    if(data==null || data.version!=1 || data.id!=id || !TryUsername(data.username,out var name,out _) || name!=data.username ||
                        !DateTime.TryParse(data.createdAtUtc,System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.RoundtripKind,out _)) throw new InvalidDataException();
                    return new LocalPlayerProfile(id,name,data.createdAtUtc);
                }
                catch(Exception error) { failure=error; }
            }
            throw new InvalidDataException("Perfil inválido.",failure);
        }
        Record ReadCredential(string directory,LocalPlayerProfile profile)
        {
            TryUsername(profile.Username,out _,out var expectedKey);
            foreach(string suffix in new[]{"",".bak"})
            {
                try
                {
                    string path=Path.Combine(directory,"credentials.bin"+suffix);
                    if(!File.Exists(path) || new FileInfo(path).Length>4096) continue;
                    using(var reader=new BinaryReader(File.OpenRead(path),Encoding.UTF8))
                    {
                        if(reader.ReadInt32()!=0x42414343 || reader.ReadInt32()!=1 || reader.ReadString()!=profile.Id || reader.ReadString()!=expectedKey) continue;
                        int count=reader.ReadInt32();
                        if(count<Iterations || count>2000000) continue;
                        var salt=reader.ReadBytes(SaltBytes); var verifier=reader.ReadBytes(KeyBytes);
                        if(salt.Length!=SaltBytes || verifier.Length!=KeyBytes || reader.BaseStream.Position!=reader.BaseStream.Length) continue;
                        return new Record { profile=profile,key=expectedKey,iterations=count,salt=salt,verifier=verifier };
                    }
                }
                catch(Exception) { /* Try the independent credential backup. */ }
            }
            throw new InvalidDataException("Credencial inválida.");
        }
        static byte[] Derive(string password,byte[] salt,int count)
        { using(var algorithm=new Rfc2898DeriveBytes(password,salt,count,HashAlgorithmName.SHA256)) return algorithm.GetBytes(KeyBytes); }
        static bool Equal(byte[] left,byte[] right)
        { int difference=left.Length^right.Length; for(int i=0;i<Math.Min(left.Length,right.Length);i++) difference|=left[i]^right[i]; return difference==0; }
        public async Task<AccountResult> CreateAsync(string username,string password,string confirmation)
        {
            if(busy) return new AccountResult(null,"Aguarde a operação atual.");
            if(!TryUsername(username,out string display,out string key)) return new AccountResult(null,"Informe um nome com 1 a 24 letras, números, espaços, ponto, hífen ou sublinhado.");
            if(string.IsNullOrWhiteSpace(password) || password.Length>128) return new AccountResult(null,"Informe uma senha não vazia com até 128 caracteres.");
            if(password!=confirmation) return new AccountResult(null,"As senhas não são iguais.");
            busy=true;
            try
            {
                Reload();
                if(records.ContainsKey(key)) return new AccountResult(null,"Este nome de usuário já existe nesta instalação.");
                if(damaged) return new AccountResult(null,"Não foi possível validar todos os perfis. A criação foi suspensa para preservar as contas existentes. Você pode jogar como convidado.");
                byte[] salt=new byte[SaltBytes]; using(var random=RandomNumberGenerator.Create()) random.GetBytes(salt);
                byte[] verifier=await Task.Run(()=>Derive(password,salt,Iterations));
                Directory.CreateDirectory(root);
                // Serialize creation across store instances/processes; recheck uniqueness after the expensive derivation.
                using(var creationLock=new FileStream(Path.Combine(root,"creation.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None))
                {
                    Reload();
                    if(damaged) return new AccountResult(null,"Não foi possível validar os perfis locais. Nenhuma conta foi sobrescrita.");
                    if(records.ContainsKey(key)) return new AccountResult(null,"Este nome de usuário já existe nesta instalação.");
                    string id=Guid.NewGuid().ToString("N"),date=DateTime.UtcNow.ToString("O");
                    string stage=Path.Combine(root,"pending-"+id),destination=Path.Combine(root,id);
                    try
                    {
                        Directory.CreateDirectory(stage);
                        File.WriteAllText(Path.Combine(stage,"profile.json"),JsonUtility.ToJson(new ProfileFile {version=1,id=id,username=display,createdAtUtc=date},true));
                        using(var stream=new FileStream(Path.Combine(stage,"credentials.bin"),FileMode.CreateNew,FileAccess.Write,FileShare.None))
                        {
                            using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))
                            { writer.Write(0x42414343);writer.Write(1);writer.Write(id);writer.Write(key);writer.Write(Iterations);writer.Write(salt);writer.Write(verifier);writer.Flush(); }
                            stream.Flush(true);
                        }
                        File.Copy(Path.Combine(stage,"profile.json"),Path.Combine(stage,"profile.json.bak"));
                        File.Copy(Path.Combine(stage,"credentials.bin"),Path.Combine(stage,"credentials.bin.bak"));
                        Directory.Move(stage,destination); // Publish only complete records; usernames never become filesystem paths.
                        var profile=new LocalPlayerProfile(id,display,date); Reload(); return new AccountResult(profile);
                    }
                    finally
                    {
                        // Only remove this operation's known staging files, never an existing profile directory.
                        try { if(Directory.Exists(stage)) { foreach(string file in new[]{"profile.json","profile.json.bak","credentials.bin","credentials.bin.bak"}) File.Delete(Path.Combine(stage,file)); Directory.Delete(stage); } } catch { }
                    }
                }
            }
            catch(Exception) { return new AccountResult(null,"Não foi possível salvar a conta neste dispositivo. Seus perfis existentes foram preservados."); }
            finally { busy=false; }
        }
        public async Task<AccountResult> LoginAsync(string username,string password)
        {
            if(busy) return new AccountResult(null,"Aguarde a operação atual.");
            busy=true;
            try
            {
                Reload(); TryUsername(username,out _,out string key);
                Record record=null; if(key!=null) records.TryGetValue(key,out record);
                // Unknown names also perform derivation; neither UI nor error messages reveal which credential failed.
                string candidate=password??""; if(candidate.Length>128) candidate="";
                byte[] derived=await Task.Run(()=>Derive(candidate,record!=null?record.salt:new byte[SaltBytes],record!=null?record.iterations:Iterations));
                bool matched=Equal(derived,record!=null?record.verifier:new byte[KeyBytes]);
                if(record==null || !matched || string.IsNullOrWhiteSpace(password)) return new AccountResult(null,InvalidCredentials);
                return new AccountResult(record.profile);
            }
            catch(Exception) { return new AccountResult(null,InvalidCredentials); }
            finally { busy=false; }
        }
    }
}
