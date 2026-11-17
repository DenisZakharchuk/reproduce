using System.Text.Json.Serialization;

namespace DataProvider.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SyncLockStatus
{
    Running,
    Done,
    Failed
}
