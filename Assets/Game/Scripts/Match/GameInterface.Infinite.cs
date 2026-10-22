using System;
using UnityEngine;
using UnityEngine.UI;
using Bellator.Classes;

namespace Bellator.Match
{
    public sealed partial class GameInterface
    {
        GameObject modePanel, rankingPanel, infiniteResults;
        Text scoreLabel, infiniteSummary, saveStatus;
        Button saveResult;
        bool resultsPresented;

        void OpenModeSelection()
        {
            home.SetActive(false);
            if(modePanel == null)
            {
                modePanel = Panel(canvas.transform, "ModeSelection", 0, 0, 1, 1, Ink);
                Label(modePanel.transform, "Title", "SELECIONAR MODO", .08f, .77f, .92f, .92f, 42, Gold);
                MakeButton(modePanel.transform, "StoryModeButton", "MODO HISTÓRIA", .15f, .57f, .85f, .69f, () => StartMode(GameMode.Story));
                Label(modePanel.transform, "StoryDescription", "Enfrente cinco ondas de gladiadores e conquiste sua liberdade.", .15f, .47f, .85f, .56f, 22, Parchment);
                MakeButton(modePanel.transform, "InfiniteModeButton", "MODO INFINITO", .15f, .29f, .85f, .41f, () => StartMode(GameMode.Infinite));
                Label(modePanel.transform, "InfiniteDescription", "Sobreviva a ondas intermináveis de inimigos e alcance o topo do ranking.", .15f, .19f, .85f, .28f, 22, Parchment);
                MakeButton(modePanel.transform, "ModeBackButton", "VOLTAR", .33f, .05f, .67f, .13f, CloseModeSelection);
            }
            modePanel.SetActive(true); Focus(modePanel.transform.Find("StoryModeButton").gameObject);
        }
        void CloseModeSelection()
        { modePanel.SetActive(false); home.SetActive(true); Focus(home.transform.Find("PlayButton").gameObject); }
        void StartMode(GameMode mode)
        { if(loading) return; GameModeManager.Select(mode); Load(PlayerClassController.ArenaScene); }

        void OpenRanking()
        {
            if(home != null) home.SetActive(false);
            if(infiniteResults != null) infiniteResults.SetActive(false);
            if(rankingPanel != null) { rankingPanel.SetActive(false); Destroy(rankingPanel); }
            rankingPanel = Panel(canvas.transform, "LocalRanking", 0, 0, 1, 1, Ink);
            Label(rankingPanel.transform, "Title", "RANKING • MODO INFINITO", .06f, .84f, .94f, .96f, 38, Gold);
            var ranking = game != null && game.Ranking != null ? game.Ranking : new LocalRankingManager();
            ranking.Reload();
            string[] headers = { "POS.", "NOME", "GLADIADOR", "PONTOS", "ONDA", "TEMPO" };
            float[] columns = { .025f, .095f, .32f, .51f, .70f, .80f, .975f };
            for(int c = 0; c < headers.Length; c++) Label(rankingPanel.transform, "Header" + c, headers[c], columns[c], .76f, columns[c+1], .82f, 19, Gold);
            for(int i = 0; i < ranking.Entries.Count; i++)
            {
                var entry = ranking.Entries[i]; float top = .75f - i * .056f;
                var row = Panel(rankingPanel.transform, "RankingRow" + i, .02f, top - .052f, .98f, top, i < 3 ? new Color(.32f, .20f, .12f) : new Color(.19f, .145f, .105f));
                string[] values = { (i+1).ToString(), entry.playerName, entry.characterName, entry.score.ToString("N0"), entry.highestWave.ToString(), MatchStatistics.FormatTime(entry.survivalSeconds) };
                for(int c = 0; c < values.Length; c++)
                {
                    var text = Label(row.transform, "Column" + c, values[c], (columns[c]-.02f)/.96f, 0, (columns[c+1]-.02f)/.96f, 1, 18, i < 3 ? Gold : Parchment);
                    text.supportRichText = false;
                }
            }
            if(ranking.Entries.Count == 0) Label(rankingPanel.transform, "EmptyRanking", "Nenhum gladiador entrou para a história. Seja o primeiro!", .1f, .35f, .9f, .65f, 27, Parchment);
            if(!string.IsNullOrEmpty(ranking.LastError)) Label(rankingPanel.transform, "RankingWarning", "Não foi possível ler todos os dados. A lista disponível está exibida.", .06f, .13f, .94f, .18f, 15, Parchment);
            var back = MakeButton(rankingPanel.transform, "RankingBackButton", "VOLTAR", .33f, .035f, .67f, .115f, CloseRanking);
            Focus(back.gameObject);
        }
        void CloseRanking()
        {
            rankingPanel.SetActive(false);
            if(game == null) { home.SetActive(true); Focus(home.transform.Find("RankingButton").gameObject); }
            else { infiniteResults.SetActive(true); Focus(infiniteResults.transform.Find("ResultsCard/RetryInfiniteButton").gameObject); }
        }

        void BuildInfiniteMatch()
        {
            var scorePanel = Panel(hud.transform, "InfiniteScorePanel", .02f, .64f, .35f, .74f, Ink);
            scoreLabel = Label(scorePanel.transform, "ScoreAndTime", "", .04f, .05f, .96f, .95f, 18, Gold);
            infiniteResults = Panel(canvas.transform, "InfiniteResults", 0, 0, 1, 1, new Color(0, 0, 0, .90f));
            var card = Panel(infiniteResults.transform, "ResultsCard", .15f, .06f, .85f, .94f, Ink);
            Label(card.transform, "Title", "FIM DA SOBREVIVÊNCIA", .04f, .85f, .96f, .97f, 37, Gold);
            infiniteSummary = Label(card.transform, "FinalStatistics", "", .06f, .48f, .94f, .84f, 23, Parchment);
            saveResult = MakeButton(card.transform, "SaveScoreButton", "TENTAR REGISTRAR NOVAMENTE", .18f, .28f, .82f, .36f, SaveResult);
            saveResult.gameObject.SetActive(false);
            saveStatus = Label(card.transform, "SaveStatus", "", .04f, .15f, .96f, .27f, 18, Parchment);
            MakeButton(card.transform, "RetryInfiniteButton", "JOGAR NOVAMENTE", .03f, .04f, .34f, .13f, () => NavigateResult(() => game.Restart()));
            MakeButton(card.transform, "ResultsRankingButton", "VER RANKING", .36f, .04f, .64f, .13f, () => NavigateResult(OpenRanking));
            MakeButton(card.transform, "ResultsMenuButton", "MENU PRINCIPAL", .66f, .04f, .97f, .13f, () => NavigateResult(() => game.ReturnToMenu()));
            infiniteResults.SetActive(false);
        }
        void SaveResult()
        {
            if(!game.CanRecordResult) { saveStatus.text="Crie uma conta para registrar seus recordes no ranking.";saveResult.gameObject.SetActive(false);return; }
            bool saved = game.SaveInfiniteResult();
            saveStatus.text = saved ? "Resultado registrado para "+game.FinalStatistics.playerName+".\nOs dez melhores aparecem no ranking." : "Falha ao salvar. Tente registrar novamente. " + game.RankingError;
            saveStatus.supportRichText=false;saveResult.gameObject.SetActive(!saved);
        }
        void NavigateResult(Action action)
        {
            if(game.Loading) return;
            if(game.CanRecordResult && !game.ResultSaved) SaveResult();
            action();
        }
        void UpdateInfiniteMatch(bool over)
        {
            var stats = game.Statistics;
            scoreLabel.text = "PONTOS  " + stats.score.ToString("N0") + "  •  " + MatchStatistics.FormatTime(stats.survivalSeconds) +
                "\n"+(game.CanRecordResult?"SEU RECORDE  "+game.Ranking.BestForProfile(stats.profileId).ToString("N0"):"CONVIDADO • SEM REGISTRO NO RANKING");
            bool rankingVisible = rankingPanel != null && rankingPanel.activeSelf;
            infiniteResults.SetActive(over && !rankingVisible);
            if(!over || resultsPresented) return;
            resultsPresented = true;
            var final = game.FinalStatistics;
            infiniteSummary.text = (game.NewRecord ? "NOVO RECORDE!\n" : "") +
                "PONTUAÇÃO  " + final.score.ToString("N0") + "\n" +
                "Maior onda: " + final.highestWave + "   •   Concluídas: " + final.wavesCompleted + "\n" +
                "Inimigos derrotados: " + final.enemiesDefeated + "\n" +
                "Sobrevivência: " + MatchStatistics.FormatTime(final.survivalSeconds) + "\n" +
                "Gladiador: " + final.characterName;
            SaveResult();
            Focus(infiniteResults.transform.Find("ResultsCard/RetryInfiniteButton").gameObject);
        }
    }
}
