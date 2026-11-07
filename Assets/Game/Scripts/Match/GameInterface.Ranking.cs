using UnityEngine;

namespace Bellator.Match
{
    public sealed partial class GameInterface
    {
        void BuildDynamicRanking()
        {
            BellatorUI.Artwork(rankingPanel,"MainMenu"); BellatorUI.Logo(rankingPanel.transform,.80f,.995f);
            BellatorUI.Title(rankingPanel.transform,"RANKING · ONDAS INFINITAS",.69f,.785f);
            var table=BellatorUI.Frame(rankingPanel.transform,"RankingFrame",.165f,.17f,.835f,.66f);
            var ranking=game!=null&&game.Ranking!=null?game.Ranking:new LocalRankingManager(); ranking.Reload();
            string[] headers={"POS.","JOGADOR","GLADIADOR","PONTOS","ONDA","TEMPO"};
            float[] columns={.025f,.10f,.34f,.56f,.74f,.84f,.98f};
            for(int c=0;c<headers.Length;c++) BellatorUI.Label(table.transform,"Header"+c,headers[c],columns[c],.89f,columns[c+1],.98f,23);
            BellatorUI.Rule(table.transform,.88f,.025f,.975f);
            for(int i=0;i<10;i++)
            {
                float top=.865f-i*.077f;
                var row=BellatorUI.Rect(table.transform,"RankingRow"+i,.02f,top-.073f,.98f,top);
                if(i>=ranking.Entries.Count) continue; // Intentional blank slots, never fabricated records.
                var entry=ranking.Entries[i];
                string[] values={(i+1).ToString(),entry.playerName,entry.characterName,entry.score.ToString("N0"),entry.highestWave.ToString(),MatchStatistics.FormatTime(entry.survivalSeconds)};
                for(int c=0;c<values.Length;c++)
                { var text=BellatorUI.Label(row.transform,"Column"+c,values[c],(columns[c]-.02f)/.96f,0,(columns[c+1]-.02f)/.96f,1,24,false); text.color=i<3?BellatorUI.Parchment:new Color(.80f,.76f,.67f); }
            }
            BellatorUI.Label(rankingPanel.transform,"RankingNotice",!string.IsNullOrEmpty(ranking.LastError)?"Não foi possível ler todos os dados. A lista disponível está exibida.":"Ranking local · Apenas perfis autenticados registram pontuações.",.22f,.12f,.78f,.16f,21,false);
            var back=BellatorUI.Button(rankingPanel.transform,"RankingBackButton","‹  VOLTAR",.02f,.035f,.18f,.103f,CloseRanking); Focus(back.gameObject);
        }
    }
}
