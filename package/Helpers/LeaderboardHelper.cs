using System;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace TinyNakama.Helpers
{
    public sealed class LeaderboardHelper : NakamaHelper
    {
        public LeaderboardHelper(NakamaClient client) : base(client) { }

        public async Task<NakamaLeaderboardList> Records(string leaderboardId, int limit = 50, string cursor = null)
        {
            var path = $"/v2/leaderboard/{Uri.EscapeDataString(leaderboardId)}?limit={limit}";
            if (!string.IsNullOrEmpty(cursor)) path += $"&cursor={Uri.EscapeDataString(cursor)}";
            var response = await client.SendAsync(UnityWebRequest.kHttpVerbGET, path);
            return NakamaClient.Deserialize<NakamaLeaderboardList>(response, "leaderboard list");
        }

        public async Task<NakamaLeaderboardList> GetAroundOwner(string leaderboardId, string ownerId, int limit = 3)
        {
            var path = $"/v2/leaderboard/{Uri.EscapeDataString(leaderboardId)}/around/{Uri.EscapeDataString(ownerId)}?limit={limit}";
            var response = await client.SendAsync(UnityWebRequest.kHttpVerbGET, path);
            return NakamaClient.Deserialize<NakamaLeaderboardList>(response, "leaderboard around owner");
        }
    }


    [Serializable]
    public sealed class NakamaLeaderboardRecord
    {
        public string leaderboardId;
        public string ownerId;
        public string username;
        public long score;
        public long subscore;
        public int rank;
        public string metadata;
        public string createTime;
        public string updateTime;
        public string expiryTime;
    }

    [Serializable]
    public sealed class NakamaLeaderboardList
    {
        public NakamaLeaderboardRecord[] records;
        public string nextCursor;
        public string prevCursor;
    }
}