using Bellator.Classes;
using UnityEngine;
using UnityEngine.UI;

namespace Bellator.Match
{
    public sealed partial class GameInterface
    {
        void BrowseCharacters() { SelectionNavigation.Browse(); Load(PlayerClassController.SelectionScene); }
        bool BuildArtworkHome()
        {
            BellatorUI.Artwork(home,"MainMenu"); BellatorUI.Logo(home.transform,.72f,.99f);
            var card=BellatorUI.Frame(home.transform,"MenuFrame",.318f,.09f,.682f,.70f);
            BellatorUI.Rule(card.transform,.95f); BellatorUI.Rule(card.transform,.045f);
            string[] names={"PlayButton","OnlineHomeButton","SelectCharacterButton","RankingButton","SettingsButton","QuitButton"};
            string[] labels={"JOGAR","MULTIPLAYER","PERSONAGENS","RANKING","CONFIGURAÇÕES","SAIR"};
            System.Action[] actions={Play,()=>Bellator.Multiplayer.OnlineGame.Open(),BrowseCharacters,OpenRanking,OpenSettings,Quit};
            for(int i=0;i<names.Length;i++)
            {
                float bottom=.566f-i*.076f; var b=BellatorUI.Button(home.transform,names[i],labels[i],.361f,bottom,.639f,bottom+.060f,actions[i]);
                var icon=BellatorUI.Rect(b.transform,"MenuIcon",.10f,.19f,.19f,.81f).AddComponent<BellatorMenuIcon>(); icon.kind=i==5?5:i>1?i-1:i; icon.color=BellatorUI.Parchment; icon.raycastTarget=false;
                var caption=b.transform.Find("Label").GetComponent<RectTransform>(); caption.anchorMin=new Vector2(.24f,.08f); caption.anchorMax=new Vector2(.965f,.92f);
            }
            profileLabel=Label(home.transform,"ConnectedPlayer","",.25f,.015f,.75f,.065f,24,Parchment);
            BellatorUI.Button(home.transform,"SwitchAccountButton","TROCAR CONTA",.79f,.925f,.97f,.98f,()=>PlayerSession.Logout());
            Focus(home.transform.Find("PlayButton").gameObject); return true;
        }
        bool BuildArtworkModes()
        {
            BellatorUI.Artwork(modePanel,"GameModes"); BellatorUI.Logo(modePanel.transform,.80f,.995f);
            BellatorUI.Title(modePanel.transform,"MODOS DE JOGO",.69f,.785f);
            string[] titles={"TRADICIONAL","ONDAS INFINITAS","MULTIPLAYER"};
            string[] descriptions={"Enfrente inimigos em combates definidos e conquiste a vitória.","Sobreviva a ondas progressivamente mais difíceis e busque a maior pontuação.","Combates 1v1 online.\nCrie uma sala ou entre com um código."};
            for(int i=0;i<3;i++)
            {
                float left=.168f+i*.226f;
                var card=BellatorUI.Frame(modePanel.transform,"ModeCard"+i,left,.108f,left+.213f,.661f);
                var art=BellatorUI.Rect(card.transform,"ModeIllustration",.055f,.57f,.945f,.95f).AddComponent<Image>();
                art.sprite=Resources.Load<Sprite>("BellatorUI/Elements/Mode"+i); art.raycastTarget=false;
                BellatorUI.Label(card.transform,"ModeTitle",titles[i],.04f,.45f,.96f,.55f,36);
                BellatorUI.Rule(card.transform,.425f);
                BellatorUI.Label(card.transform,"Description",descriptions[i],.09f,.19f,.91f,.39f,26,false);
                // Keep the existing mode controls directly under ModeSelection for navigation compatibility.
                var b=BellatorUI.Button(modePanel.transform,i==0?"StoryModeButton":i==1?"InfiniteModeButton":"MultiplayerButton","JOGAR",left+.025f,.14f,left+.188f,.195f,i==0?()=>StartMode(GameMode.Story):i==1?()=>StartMode(GameMode.Infinite):()=>Bellator.Multiplayer.OnlineGame.Open());
                b.interactable=true;
            }
            BellatorUI.Button(modePanel.transform,"ModeBackButton","‹  VOLTAR",.02f,.035f,.18f,.103f,CloseModeSelection); return true;
        }
    }
}
