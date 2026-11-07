using Bellator.Classes;
using UnityEngine;

namespace Bellator.Match
{
    public sealed partial class GameInterface
    {
        GameObject creditsPanel;
        void BrowseCharacters() { SelectionNavigation.Browse(); Load(PlayerClassController.SelectionScene); }
        bool BuildArtworkHome()
        {
            if (!BellatorUI.Artwork(home,"MainMenu")) return false;
            BellatorUI.Button(home.transform,"PlayButton","",.361f,.554f,.639f,.631f,Play,true);
            BellatorUI.Button(home.transform,"SelectCharacterButton","",.365f,.468f,.635f,.539f,BrowseCharacters,true);
            BellatorUI.Button(home.transform,"RankingButton","",.365f,.382f,.635f,.453f,OpenRanking,true);
            BellatorUI.Button(home.transform,"SettingsButton","",.365f,.296f,.635f,.368f,OpenSettings,true);
            BellatorUI.Button(home.transform,"CreditsButton","",.365f,.211f,.635f,.282f,OpenCredits,true);
            BellatorUI.Button(home.transform,"QuitButton","",.365f,.123f,.635f,.196f,Quit,true);
            profileLabel = Label(home.transform,"ConnectedPlayer","",.25f,.015f,.75f,.065f,24,Parchment);
            BellatorUI.Button(home.transform,"SwitchAccountButton","TROCAR CONTA",.79f,.925f,.97f,.98f,()=>PlayerSession.Logout());
            Focus(home.transform.Find("PlayButton").gameObject); return true;
        }
        bool BuildArtworkModes()
        {
            if (!BellatorUI.Artwork(modePanel,"GameModes")) return false;
            BellatorUI.Button(modePanel.transform,"StoryModeButton","",.208f,.168f,.479f,.238f,()=>StartMode(GameMode.Story),true);
            BellatorUI.Button(modePanel.transform,"InfiniteModeButton","",.523f,.168f,.795f,.238f,()=>StartMode(GameMode.Infinite),true);
            BellatorUI.Button(modePanel.transform,"ModeBackButton","",.024f,.922f,.155f,.978f,CloseModeSelection,true);
            return true;
        }
        void OpenCredits()
        {
            home.SetActive(false);
            if (creditsPanel == null)
            {
                creditsPanel = Panel(canvas.transform,"CreditsPanel",0,0,1,1,Ink);
                BellatorUI.Label(creditsPanel.transform,"Title","BELLATOR",.15f,.72f,.85f,.88f,64);
                BellatorUI.Label(creditsPanel.transform,"Credits","Da Arena à Liberdade\n\nDesenvolvido com Unity\n\nCréditos da equipe: a definir pelo autor.",.15f,.30f,.85f,.70f,32);
                BellatorUI.Button(creditsPanel.transform,"CreditsBackButton","VOLTAR",.33f,.10f,.67f,.19f,CloseCredits);
            }
            creditsPanel.SetActive(true); Focus(creditsPanel.transform.Find("CreditsBackButton").gameObject);
        }
        void CloseCredits() { creditsPanel.SetActive(false); home.SetActive(true); Focus(home.transform.Find("CreditsButton").gameObject); }
    }
}
