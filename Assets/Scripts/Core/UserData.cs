using UnityEngine;

[CreateAssetMenu(fileName = "UserData", menuName = "HandCricket/User Data")]
public class UserData : ScriptableObject
{
    public string username;
    public int coins;
    public int level = 1;
    public int xp;

    [Header("Career Stats")]
    public int Matches;
    public int Wins;
    public int Losses;
    public int Draws;

    public int TotalRuns;
    public int HighestScore;
    public int BallsFaced;
    public int FourCount;
    public int SixCount;

    public int TotalWickets;
    public int BallsBowled;
    public int RunsConceded;
    public int BestWickets;
    public int BestRuns;

    public event System.Action OnDataChanged;

    public void SetStats(string jsonStats)
    {
        if (string.IsNullOrEmpty(jsonStats)) return;

        try
        {
            var data = JsonUtility.FromJson<UserStatsData>(jsonStats);
            this.Matches = data.matches;
            this.Wins = data.wins;
            this.Losses = data.losses;
            this.Draws = data.draws;
            this.TotalRuns = data.total_runs;
            this.HighestScore = data.highest_score;
            this.BallsFaced = data.balls_faced;
            this.FourCount = data.four_count;
            this.SixCount = data.six_count;
            this.TotalWickets = data.total_wickets;
            this.BallsBowled = data.balls_bowled;
            this.RunsConceded = data.runs_conceded;
            this.BestWickets = data.best_wickets;
            this.BestRuns = data.best_runs;

            OnDataChanged?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to parse Stats JSON: " + e.Message);
        }
    }

    public void Clear()
    {
        username = "";
        coins = 0;
        level = 1;
        xp = 0;

        Matches = Wins = Losses = Draws = 0;
        TotalRuns = HighestScore = BallsFaced = FourCount = SixCount = 0;
        TotalWickets = BallsBowled = RunsConceded = BestWickets = BestRuns = 0;
    }

    public void SetFromWallet(string jsonWallet)
    {
        if (string.IsNullOrEmpty(jsonWallet)) return;

        try
        {
            var data = JsonUtility.FromJson<WalletData>(jsonWallet);
            this.coins = data.coins;
            this.level = data.level;
            this.xp = data.xp;

            OnDataChanged?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to parse Wallet JSON: " + e.Message);
        }
    }

    [System.Serializable]
    private class WalletData
    {
        public int coins;
        public int level;
        public int xp;
    }

    [System.Serializable]
    private class UserStatsData
    {
        public int matches;
        public int wins;
        public int losses;
        public int draws;
        public int total_runs;
        public int highest_score;
        public int balls_faced;
        public int four_count;
        public int six_count;
        public int total_wickets;
        public int balls_bowled;
        public int runs_conceded;
        public int best_wickets;
        public int best_runs;
    }
}
