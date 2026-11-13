using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bellator.Match
{
    public sealed partial class GameInterface
    {
        GameObject accessRoot,confirmPasswordRoot,accountOptions,guestNotice,registrationTitle,emailRoot,localNotice;
        Text profileLabel;
        TextMeshProUGUI accountError;
        TMP_InputField accountUsername,accountPassword,accountConfirmation,accountEmail;
        Button accountSubmit,accountBack,loginTab,createTab,guestButton;
        Toggle rememberUsername;
        bool creatingAccount,accountBusy;

        void BuildAccess()
        {
            accessRoot=Panel(MenuParent,"AccessScreen",0,0,1,1,Ink);
            BellatorUI.Artwork(accessRoot,"Login"); BellatorUI.Logo(accessRoot.transform,.735f,.995f);
            BellatorUI.Frame(accessRoot.transform,"AccountFrame",.268f,.10f,.732f,.705f);
            registrationTitle=BellatorUI.Label(accessRoot.transform,"RegistrationTitle","CADASTRO",.32f,.624f,.68f,.690f,46).gameObject;
            accountUsername=AccountField(accessRoot.transform,"AccountUsername","Usuário ou e-mail",.323f,.545f,.677f,.62f,false);
            accountPassword=AccountField(accessRoot.transform,"AccountPassword","Senha",.323f,.445f,.677f,.52f,true);
            accountEmail=AccountField(accessRoot.transform,"AccountEmail","E-mail",.323f,.46f,.677f,.52f,false); emailRoot=accountEmail.gameObject;
            accountEmail.characterLimit=254; accountEmail.contentType=TMP_InputField.ContentType.EmailAddress;
            accountConfirmation=AccountField(accessRoot.transform,"AccountConfirmation","Confirmar senha",.323f,.30f,.677f,.36f,true); confirmPasswordRoot=accountConfirmation.gameObject;
            accountOptions=BellatorUI.Rect(accessRoot.transform,"AccountOptions",.323f,.383f,.677f,.431f);
            rememberUsername=BellatorUI.Rect(accountOptions.transform,"RememberUsername",0,0,.49f,1).AddComponent<Toggle>();
            var hit=rememberUsername.gameObject.AddComponent<Image>(); hit.color=Color.clear;
            BellatorUI.Frame(rememberUsername.transform,"CheckFrame",.005f,.20f,.095f,.80f);
            var tick=BellatorUI.Rect(rememberUsername.transform,"Check",.025f,.33f,.075f,.67f).AddComponent<Image>(); tick.color=Gold; tick.raycastTarget=false;
            rememberUsername.graphic=tick; rememberUsername.targetGraphic=hit;
            BellatorUI.Label(rememberUsername.transform,"Label","Lembrar de mim",.12f,0,1,1,22,false);
            rememberUsername.SetIsOnWithoutNotify(PlayerPrefs.GetInt("Bellator.RememberUsername",0)==1);
            BellatorUI.Button(accountOptions.transform,"PasswordHelp","Esqueceu a senha?",.51f,0,1,1,()=>accountError.text="Contas locais: não há recuperação de senha por e-mail. Use outra conta ou entre como convidado.");
            accountSubmit=BellatorUI.Button(accessRoot.transform,"AccountSubmitButton","ENTRAR",.357f,.292f,.643f,.369f,SubmitAccount);
            createTab=BellatorUI.Button(accessRoot.transform,"CreateAccountButton","CADASTRAR",.304f,.178f,.49f,.247f,()=>OpenAccountForm(true));
            guestButton=BellatorUI.Button(accessRoot.transform,"GuestButton","JOGAR COMO\nCONVIDADO",.51f,.178f,.696f,.247f,()=>PlayerSession.EnterGuest());
            guestNotice=BellatorUI.Label(accessRoot.transform,"GuestRankingNotice","Convidados não participam do ranking.",.49f,.128f,.71f,.17f,20,false).gameObject;
            loginTab=BellatorUI.Button(accessRoot.transform,"LoginButton","JÁ TENHO CONTA  ·  ENTRAR",.35f,.115f,.65f,.164f,()=>OpenAccountForm(false));
            accountBack=BellatorUI.Button(accessRoot.transform,"AccountBackButton","‹  VOLTAR",.02f,.035f,.18f,.103f,CloseAccountForm);
            var messagePanel=Panel(accessRoot.transform,"AccountMessage",.25f,.01f,.75f,.075f,new Color(.06f,.045f,.03f,.96f)); messagePanel.GetComponent<Image>().raycastTarget=false;
            accountError=BellatorUI.Label(messagePanel.transform,"AccountError","",.02f,.04f,.98f,.96f,23,false);
            localNotice=BellatorUI.Label(accessRoot.transform,"LocalAccountsNotice","Perfis locais neste dispositivo",.30f,.708f,.70f,.735f,18,false).gameObject;
            accountPassword.onSubmit.AddListener(_=>{ if(!creatingAccount) SubmitAccount(); else Focus(accountConfirmation.gameObject); });
            accountConfirmation.onSubmit.AddListener(_=>SubmitAccount());
            OpenAccountForm(false);
        }
        TMP_InputField AccountField(Transform parent,string name,string placeholder,float x,float y,float r,float t,bool password)
        {
            var root=BellatorUI.Rect(parent,name,x,y,r,t); var frame=root.AddComponent<BellatorFrame>(); frame.color=new Color(.06f,.047f,.033f); frame.corner=7; frame.border=2;
            var input=root.AddComponent<TMP_InputField>(); input.targetGraphic=frame;
            var viewport=BellatorUI.Rect(root.transform,"Viewport",.035f,.10f,password?.84f:.965f,.90f).GetComponent<RectTransform>();
            viewport.gameObject.AddComponent<RectMask2D>(); input.textViewport=viewport;
            input.textComponent=BellatorUI.Label(viewport,"Text","",0,0,1,1,28,false); input.textComponent.alignment=TextAlignmentOptions.MidlineLeft;
            var hint=BellatorUI.Label(viewport,"Placeholder",placeholder,0,0,1,1,28,false); hint.alignment=TextAlignmentOptions.MidlineLeft; hint.color=new Color(.71f,.64f,.52f); input.placeholder=hint;
            input.characterLimit=password?128:254; input.lineType=TMP_InputField.LineType.SingleLine;
            input.contentType=password?TMP_InputField.ContentType.Password:TMP_InputField.ContentType.Standard;
            if(password)
            {
                var reveal=BellatorUI.Button(root.transform,"RevealPassword","VER",.86f,.15f,.98f,.85f,null);
                reveal.onClick.AddListener(()=> { bool show=input.contentType==TMP_InputField.ContentType.Password; input.contentType=show?TMP_InputField.ContentType.Standard:TMP_InputField.ContentType.Password; reveal.GetComponentInChildren<TextMeshProUGUI>().text=show?"OCULTAR":"VER"; input.ForceLabelUpdate(); });
            }
            return input;
        }
        void SessionChanged()
        {
            if(home==null) return;
            accountBusy=false; ClearCredentials();
            if(modePanel!=null) modePanel.SetActive(false); if(rankingPanel!=null) rankingPanel.SetActive(false);
            if(creditsPanel!=null) creditsPanel.SetActive(false); if(settings!=null) settings.SetActive(false); settingsVisible=false;
            accessRoot.SetActive(!PlayerSession.HasSession); home.SetActive(PlayerSession.HasSession);
            profileLabel.text=PlayerSession.IsAccount?"Jogador: "+PlayerSession.Username:PlayerSession.HasSession?"Jogando como convidado":"Entre na conta ou jogue como convidado"; profileLabel.supportRichText=false;
            if(!PlayerSession.HasSession) OpenAccountForm(false); else Focus(home.transform.Find("PlayButton").gameObject);
        }
        static void Place(Component component,float x,float y,float r,float t)
        { var rect=component.GetComponent<RectTransform>(); rect.anchorMin=new Vector2(x,y); rect.anchorMax=new Vector2(r,t); rect.offsetMin=rect.offsetMax=Vector2.zero; }
        void OpenAccountForm(bool create)
        {
            if(accountBusy) return;
            creatingAccount=create; ClearCredentials(); confirmPasswordRoot.SetActive(create); emailRoot.SetActive(create); registrationTitle.SetActive(create);
            accountOptions.SetActive(!create); createTab.gameObject.SetActive(!create); guestButton.gameObject.SetActive(!create); guestNotice.SetActive(!create); loginTab.gameObject.SetActive(create);
            accountError.text=""; accountSubmit.GetComponentInChildren<TextMeshProUGUI>().text=create?"CADASTRAR":"ENTRAR";
            Place(accountUsername,.323f,create?.54f:.545f,.677f,create?.60f:.62f);
            Place(accountPassword,.323f,create?.38f:.445f,.677f,create?.44f:.52f);
            Place(accountSubmit,.342f,create?.18f:.292f,.658f,create?.258f:.369f);
            ((TextMeshProUGUI)accountUsername.placeholder).text=create?"Nome de usuário":"Usuário ou e-mail";
            if(!create&&rememberUsername.isOn) accountUsername.text=PlayerPrefs.GetString("Bellator.RememberedUsername","");
            Focus(accountUsername.gameObject);
        }
        void CloseAccountForm()
        {
            if(accountBusy) return;
            if(creatingAccount) { OpenAccountForm(false); return; }
            ClearCredentials(); accessRoot.SetActive(false); home.SetActive(true); Focus(home.transform.Find("PlayButton").gameObject);
        }
        void ClearCredentials()
        {
            if(accountPassword==null) return;
            accountUsername.text=""; accountPassword.text=""; accountConfirmation.text=""; accountEmail.text="";
            foreach(var input in new[]{accountPassword,accountConfirmation}) { input.contentType=TMP_InputField.ContentType.Password; input.ForceLabelUpdate(); input.transform.Find("RevealPassword/Label").GetComponent<TextMeshProUGUI>().text="VER"; }
        }
        void SetAccountBusy(bool busy)
        {
            accountBusy=busy; accountSubmit.interactable=accountBack.interactable=loginTab.interactable=createTab.interactable=guestButton.interactable=!busy;
            accountUsername.interactable=accountPassword.interactable=accountConfirmation.interactable=accountEmail.interactable=rememberUsername.interactable=!busy;
            foreach(var button in accessRoot.GetComponentsInChildren<Button>(true)) button.interactable=!busy;
        }
        async void SubmitAccount()
        {
            if(accountBusy) return;
            string username=accountUsername.text; SetAccountBusy(true); accountError.text=creatingAccount?"Criando perfil local...":"Verificando credenciais locais...";
            try
            {
                var store=PlayerSession.Accounts;
                var result=creatingAccount?await store.CreateAsync(username,accountPassword.text,accountConfirmation.text,accountEmail.text):await store.LoginAsync(username,accountPassword.text);
                if(this==null) return;
                accountPassword.text=""; accountConfirmation.text="";
                if(result.Success)
                {
                    PlayerPrefs.SetInt("Bellator.RememberUsername",rememberUsername.isOn?1:0);
                    if(rememberUsername.isOn) PlayerPrefs.SetString("Bellator.RememberedUsername",result.Profile.Username); else PlayerPrefs.DeleteKey("Bellator.RememberedUsername"); PlayerPrefs.Save();
                    if(PlayerSession.EnterAccount(result)) return;
                }
                accountError.text=result.Error??"Não foi possível iniciar a sessão.";
            }
            catch(Exception) { if(this!=null) accountError.text="Não foi possível acessar os perfis locais. Tente novamente ou jogue como convidado."; }
            finally { if(this!=null) SetAccountBusy(false); }
        }
    }
}
