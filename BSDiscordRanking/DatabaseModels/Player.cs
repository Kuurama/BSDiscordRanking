using System;
using BSDiscordRanking.DatabaseFramework;

namespace BSDiscordRanking.DatabaseModels
{
    [Table("player")]
    internal class Player : Entity<Player>
    {
        [TableField] [TablePrimaryKey] [TableAutoIncrement]
        public UInt64 ID;
        [TableField]
        public UInt64 GameUserID;
        [TableField]
        public UInt64 DiscordUserID;
        [TableField]
        public string Flag;
        [TableField]
        public string Name;
        [TableField]
        public UInt64 LastScoreTime;
        [TableField]
        public int TotalPlayCount;
        [TableField]
        public float PP;
        [TableField]
        public string Avatar;
    }
}
