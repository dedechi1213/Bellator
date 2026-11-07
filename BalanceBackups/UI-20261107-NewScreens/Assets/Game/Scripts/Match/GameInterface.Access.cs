using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bellator.Match
{
    public sealed partial class GameInterface
    {
        GameObject accessRoot, confirmPasswordRoot;
        Text profileLabel;
        TextMeshProUGUI accountError;
        TMP_InputField accountUsername, accountPassword, accountConfirmation;
        Button accountSubmit, accountBack, loginTab, createTab, guestButton;
        Toggle rememberUsername;
        bool creatingAccount, accountBusy;

        void BuildAccess()
        {
            accessRoot = Panel(canvas.transform,"AccessScreen",0,0,1,1,Ink);
            BellatorUI.Artwork(accessRoot,"Login");
            loginTab = BellatorUI.Button(accessRoot.transform,"LoginButton","",.327f,.620f,.500f,.680f,()=>OpenAccountForm(false),true);
            createTab = BellatorUI.Button(accessRoot.transform,"CreateAccountButton","",.500f,.620f,.674f,.680f,()=>OpenAccountForm(true),true);
            accountUsername = AccountField(accessRoot.transform,"AccountUsername","Nome de usuário local",.366f,.533f,.662f,.584f,false);
            accountPassword = AccountField(accessRoot.transform,"AccountPassword","Senha",.366f,.459f,.662f,.510f,true);
            var options = Panel(accessRoot.transform,"AccountOptions",.330f,.389f,.671f,.443f,new Color(.075f,.07f,.06f));
            rememberUsername = BellatorUI.Rect(options.transform,"RememberUsername",0,0,.50f,1).AddComponent<Toggle>();
            rememberUsername.gameObject.AddComponent<Image>().color=Color.clear;
            Panel(rememberUsername.transform,"CheckFrame",.005f,.20f,.095f,.80f,Gold);
            Panel(rememberUsername.transform,"CheckInterior",.015f,.29f,.085f,.71f,Ink);
            var tick = Panel(rememberUsername.transform,"Check",.01f,.25f,.09f,.75f,Gold).GetComponent<Image>();
            rememberUsername.graphic = tick; rememberUsername.targetGraphic = tick;
            BellatorUI.Label(rememberUsername.transform,"Label","Lembrar usuário",.11f,0,1,1,20);
            rememberUsername.SetIsOnWithoutNotify(PlayerPrefs.GetInt("Bellator.RememberUsername",0)==1);
            BellatorUI.Button(options.transform,"PasswordHelp","Esqueceu a senha?",.51f,0,1,1,()=>accountError.text="Contas locais: não há recuperação de senha. Use outra conta ou entre como convidado.",true);
            BellatorUI.Label(options.transform,"HelpLabel","Esqueceu a senha?",.51f,0,1,1,20);
            confirmPasswordRoot = Panel(accessRoot.transform,"Confirmation",.330f,.389f,.671f,.443f,new Color(.075f,.07f,.06f));
            accountConfirmation = AccountField(confirmPasswordRoot.transform,"AccountConfirmation","Confirmar senha",.02f,.02f,.98f,.98f,true);
            accountSubmit = BellatorUI.Button(accessRoot.transform,"AccountSubmitButton","",.333f,.310f,.668f,.379f,SubmitAccount,true);
            var submitLabel = Panel(accountSubmit.transform,"SubmitCaption",.20f,.12f,.80f,.86f,new Color(.35f,.018f,.009f));
            BellatorUI.Label(submitLabel.transform,"Label","ENTRAR",0,0,1,1,34);
            guestButton = BellatorUI.Button(accessRoot.transform,"GuestButton","",.343f,.161f,.659f,.224f,()=>PlayerSession.EnterGuest(),true);
            accountBack = BellatorUI.Button(accessRoot.transform,"AccountBackButton","",.026f,.027f,.148f,.082f,CloseAccountForm,true);
            var messagePanel=Panel(accessRoot.transform,"AccountMessage",.332f,.241f,.668f,.292f,new Color(.065f,.06f,.05f));
            messagePanel.GetComponent<Image>().raycastTarget=false;
            accountError = BellatorUI.Label(messagePanel.transform,"AccountError","",.02f,0,.98f,1,20);
            BellatorUI.Label(accessRoot.transform,"LocalAccountsNotice","Contas locais • acesso por nome de usuário",.30f,.710f,.70f,.738f,18);
            OpenAccountForm(false);
        }
        TMP_InputField AccountField(Transform parent,string name,string placeholder,float x,float y,float r,float t,bool password)
        {
            var root = Panel(parent,name,x,y,r,t,new Color(.165f,.165f,.16f));
            var input = root.AddComponent<TMP_InputField>(); input.targetGraphic = root.GetComponent<Image>();
            var viewport = BellatorUI.Rect(root.transform,"Viewport",.025f,.06f,.975f,.94f).GetComponent<RectTransform>();
            viewport.gameObject.AddComponent<RectMask2D>(); input.textViewport = viewport;
            input.textComponent = BellatorUI.Label(viewport,"Text","",0,0,1,1,26);
            input.textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            var hint = BellatorUI.Label(viewport,"Placeholder",placeholder,0,0,1,1,26);
            hint.alignment = TextAlignmentOptions.MidlineLeft; hint.color = new Color(.68f,.68f,.65f); input.placeholder = hint;
            input.characterLimit = password ? 128 : 24; input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            return input;
        }
        void SessionChanged()
        {
            if (home == null) return;
            accountBusy = false; ClearCredentials();
            if(modePanel!=null) modePanel.SetActive(false);
            if(rankingPanel!=null) rankingPanel.SetActive(false);
            if(creditsPanel!=null) creditsPanel.SetActive(false);
            if(settings!=null) settings.SetActive(false); settingsVisible=false;
            accessRoot.SetActive(!PlayerSession.HasSession); home.SetActive(PlayerSession.HasSession);
            profileLabel.text = PlayerSession.IsAccount ? "Jogador: " + PlayerSession.Username : PlayerSession.HasSession ? "Jogando como convidado" : "Entre na conta ou jogue como convidado";
            profileLabel.supportRichText = false;
            if (!PlayerSession.HasSession) OpenAccountForm(false);
            else Focus(home.transform.Find("PlayButton").gameObject);
        }
        void OpenAccountForm(bool create)
        {
            if(accountBusy) return;
            creatingAccount=create; ClearCredentials(); confirmPasswordRoot.SetActive(create);
            accountError.text = "";
            accountSubmit.transform.Find("SubmitCaption").gameObject.SetActive(create);
            accountSubmit.transform.Find("SubmitCaption/Label").GetComponent<TextMeshProUGUI>().text=create?"CADASTRAR":"ENTRAR";
            if (!create && rememberUsername.isOn) accountUsername.text = PlayerPrefs.GetString("Bellator.RememberedUsername","");
            Focus(accountUsername.gameObject);
        }
        void CloseAccountForm()
        {
            if(accountBusy) return;
            ClearCredentials(); accessRoot.SetActive(false); home.SetActive(true); Focus(home.transform.Find("PlayButton").gameObject);
        }
        void ClearCredentials()
        {
            if(accountPassword==null) return;
            accountUsername.text=""; accountPassword.text=""; accountConfirmation.text="";
        }
        void SetAccountBusy(bool busy)
        {
            accountBusy=busy;
            accountSubmit.interactable=accountBack.interactable=loginTab.interactable=createTab.interactable=guestButton.interactable=!busy;
            accountUsername.interactable=accountPassword.interactable=accountConfirmation.interactable=rememberUsername.interactable=!busy;
        }
        async void SubmitAccount()
        {
            if(accountBusy) return;
            string username = accountUsername.text; SetAccountBusy(true);
            accountError.text=creatingAccount?"Criando perfil local...":"Verificando credenciais locais...";
            try
            {
                var store=PlayerSession.Accounts;
                var result=creatingAccount?await store.CreateAsync(username,accountPassword.text,accountConfirmation.text):await store.LoginAsync(username,accountPassword.text);
                if(this==null) return;
                accountPassword.text=""; accountConfirmation.text="";
                if(result.Success)
                {
                    PlayerPrefs.SetInt("Bellator.RememberUsername",rememberUsername.isOn?1:0);
                    if(rememberUsername.isOn) PlayerPrefs.SetString("Bellator.RememberedUsername",username); else PlayerPrefs.DeleteKey("Bellator.RememberedUsername");
                    PlayerPrefs.Save();
                    if(PlayerSession.EnterAccount(result)) return;
                }
                accountError.text=result.Error??"Não foi possível iniciar a sessão.";
            }
            catch(Exception) { if(this!=null) accountError.text="Não foi possível acessar os perfis locais. Tente novamente ou jogue como convidado."; }
            finally { if(this!=null) SetAccountBusy(false); }
        }
    }
}
