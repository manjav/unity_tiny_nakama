using System;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace TinyNakama.Helpers
{
    public sealed class LeaderboardHelper : NakamaHelper
    {
        public LeaderboardHelper(NakamaClient client) : base(client) { }

        public async Task<LeaderboardList> Records(string leaderboardId, int limit = 50, string cursor = null)
        {
            var path = $"/v2/leaderboard/{Uri.EscapeDataString(leaderboardId)}?limit={limit}";
            if (!string.IsNullOrEmpty(cursor)) path += $"&cursor={Uri.EscapeDataString(cursor)}";
            var response = await client.SendAsync(UnityWebRequest.kHttpVerbGET, path);
            var result = NakamaClient.Deserialize<LeaderboardList>(response, "leaderboard list", true);
            foreach (var record in result.records)
            {
                record.info = NakamaClient.Deserialize<LeaderboardRecordInfo>(record.metadata, "leaderboard info");
            }
            return result;
        }

        public async Task<LeaderboardList> GetAroundOwner(string leaderboardId, string ownerId, int limit = 3)
        {
            var path = $"/v2/leaderboard/{Uri.EscapeDataString(leaderboardId)}/owner/{Uri.EscapeDataString(ownerId)}?limit={limit}";
            var response = await client.SendAsync(UnityWebRequest.kHttpVerbGET, path);
            var result = NakamaClient.Deserialize<LeaderboardList>(response, "leaderboard around owner", true);
            foreach (var record in result.records)
            {
                record.info = NakamaClient.Deserialize<LeaderboardRecordInfo>(record.metadata, "leaderboard info");
            }
            return result;
        }
    }


    [Serializable]
    public sealed class LeaderboardList
    {
        public LeaderboardRecord[] records;
        public string nextCursor;
        public string prevCursor;
    }

    [Serializable]
    public sealed class LeaderboardRecord
    {
        public string leaderboardId;
        public string ownerId;
        public string username;
        public long score;
        public int rank;
        public string metadata;
        public string createTime;
        public string updateTime;
        public string expiryTime;
        public int numScore, maxNumScore;
        public LeaderboardRecordInfo info;
    }

    [Serializable]
    public sealed class LeaderboardRecordInfo
    {
        public int avatarUrl;
        public string displayName;
        public string location;
    }

}