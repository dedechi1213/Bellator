using System;
using UnityEngine;
using UnityEngine.UI;

namespace Bellator.Match
{
    public sealed partial class GameInterface
    {
        GameObject accessRoot,accessHome,accountForm,confirmPasswordRoot;
        Text profileLabel,accountTitle,accountError;
        InputField accountUsername,accountPassword,accountConfirmation;
        Button accountSubmit,accountBack;
        bool creatingAccount,accountBusy;

        void BuildAccess()
        {
            accessRoot=Panel(canvas.transform,"AccessScreen",0,0,1,1,Ink);
            accessHome=Panel(accessRoot.transform,"AccessHome",0,0,1,1,Color.clear);
            Panel(accessHome.transform,"GoldRule",.10f,.85f,.90f,.854f,Gold);
            Label(accessHome.transform,"Title","BELLATOR",.1f,.68f,.9f,.84f,72,Gold);
            Label(accessHome.transform,"Subtitle","Da Arena à Liberdade",.1f,.61f,.9f,.68f,24,Parchment);
            MakeButton(accessHome.transform,"LoginButton","ENTRAR",.33f,.47f,.67f,.55f,()=>OpenAccountForm(false));
            MakeButton(accessHome.transform,"CreateAccountButton","CRIAR CONTA",.33f,.35f,.67f,.43f,()=>OpenAccountForm(true));
            MakeButton(accessHome.transform,"GuestButton","JOGAR COMO CONVIDADO",.33f,.23f,.67f,.31f,()=>PlayerSession.EnterGuest());
            Label(accessHome.transform,"GuestNotice","Jogando como convidado, sua pontuação não contará no ranking.",.12f,.15f,.88f,.21f,18,Parchment);
            Label(accessHome.transform,"LocalAccountsNotice","Perfis locais deste dispositivo. Não há autenticação online.",.12f,.055f,.88f,.11f,16,Parchment);
            accountForm=Panel(accessRoot.transform,"AccountForm",.20f,.045f,.80f,.955f,Ink);
            accountTitle=Label(accountForm.transform,"AccountTitle","",.04f,.85f,.96f,.98f,36,Gold);
            accountUsername=AccountField(accountForm.transform,"AccountUsername","Nome de usuário",.67f,false);
            accountPassword=AccountField(accountForm.transform,"AccountPassword","Senha",.50f,true);
            confirmPasswordRoot=Panel(accountForm.transform,"Confirmation",0,.29f,1,.46f,Color.clear);
            accountConfirmation=AccountField(confirmPasswordRoot.transform,"AccountConfirmation","Confirmar senha",0,true,true);
            accountError=Label(accountForm.transform,"AccountError","",.06f,.19f,.94f,.29f,18,Parchment);
            accountError.supportRichText=false;
            accountSubmit=MakeButton(accountForm.transform,"AccountSubmitButton","",.12f,.105f,.88f,.185f,SubmitAccount);
            accountBack=MakeButton(accountForm.transform,"AccountBackButton","VOLTAR",.25f,.015f,.75f,.09f,CloseAccountForm);
            accountForm.SetActive(false);
        }
        InputField AccountField(Transform parent,string name,string title,float bottom,bool password,bool fill=false)
        {
            float labelBottom=fill?.58f:bottom+.09f,labelTop=fill?1:bottom+.15f;
            Label(parent,name+"Label",title,.08f,labelBottom,.92f,labelTop,19,Parchment);
            var root=Panel(parent,name,.08f,fill?0:bottom,.92f,fill?.56f:bottom+.085f,new Color(.29f,.23f,.18f));
            var input=root.AddComponent<InputField>();input.targetGraphic=root.GetComponent<Image>();
            input.textComponent=Label(root.transform,"Text","",.035f,.06f,.965f,.94f,22,Parchment);input.textComponent.supportRichText=false;
            input.characterLimit=password?128:24; input.lineType=InputField.LineType.SingleLine;
            input.contentType=password?InputField.ContentType.Password:InputField.ContentType.Standard;
            return input;
        }
        void SessionChanged()
        {
            if(home==null) return;
            accountBusy=false; ClearCredentials();
            if(modePanel!=null) modePanel.SetActive(false);
            if(rankingPanel!=null) rankingPanel.SetActive(false);
            if(settings!=null) settings.SetActive(false);settingsVisible=false;
            accessRoot.SetActive(!PlayerSession.HasSession);home.SetActive(PlayerSession.HasSession);
            accessHome.SetActive(true);accountForm.SetActive(false);
            profileLabel.text=PlayerSession.IsAccount?"Jogador: "+PlayerSession.Username:"Jogando como convidado";profileLabel.supportRichText=false;
            Focus(PlayerSession.HasSession?home.transform.Find("PlayButton").gameObject:accessHome.transform.Find("LoginButton").gameObject);
        }
        void OpenAccountForm(bool create)
        {
            if(accountBusy) return;
            creatingAccount=create;ClearCredentials();accountError.text="";
            accessHome.SetActive(false);accountForm.SetActive(true);confirmPasswordRoot.SetActive(create);
            accountTitle.text=create?"CRIAR CONTA LOCAL":"ENTRAR NA CONTA LOCAL";
            accountSubmit.GetComponentInChildren<Text>().text=create?"CRIAR CONTA":"ENTRAR";
            accountSubmit.interactable=accountBack.interactable=true;Focus(accountUsername.gameObject);
        }
        void CloseAccountForm()
        {
            if(accountBusy) return;
            ClearCredentials();accountForm.SetActive(false);accessHome.SetActive(true);Focus(accessHome.transform.Find("LoginButton").gameObject);
        }
        void ClearCredentials()
        {
            if(accountPassword==null) return;
            accountUsername.text="";accountPassword.text="";accountConfirmation.text="";
        }
        async void SubmitAccount()
        {
            if(accountBusy) return;
            accountBusy=true;accountSubmit.interactable=accountBack.interactable=false;
            accountUsername.interactable=accountPassword.interactable=accountConfirmation.interactable=false;
            accountError.text=creatingAccount?"Criando perfil local...":"Verificando credenciais locais...";
            try
            {
                var store=PlayerSession.Accounts;
                var result=creatingAccount?await store.CreateAsync(accountUsername.text,accountPassword.text,accountConfirmation.text):await store.LoginAsync(accountUsername.text,accountPassword.text);
                if(this==null) return;
                accountPassword.text="";accountConfirmation.text="";
                if(result.Success && PlayerSession.EnterAccount(result)) return;
                accountError.text=result.Error??"Não foi possível iniciar a sessão.";
            }
            catch(Exception) { if(this!=null) accountError.text="Não foi possível acessar os perfis locais. Tente novamente ou jogue como convidado."; }
            finally
            {
                if(this!=null)
                {
                    accountBusy=false;accountSubmit.interactable=accountBack.interactable=true;
                    accountUsername.interactable=accountPassword.interactable=accountConfirmation.interactable=true;
                }
            }
        }
    }
}
