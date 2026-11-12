// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;
using MessagePack.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// ReSharper disable UnusedMember.Global

namespace lazer_echo;

[MessagePackObject]
public class BeatmapUpdates(int[] beatmapSetIDs, int lastProcessedQueueId)
{
	[Key(0)] public int[] BeatmapSetIDs { get; set; } = beatmapSetIDs;

	[Key(1)] public int LastProcessedQueueID { get; set; } = lastProcessedQueueId;
}

public class ModSettingsDictionaryFormatter : IMessagePackFormatter<Dictionary<string, object>?>
{
	public void Serialize(ref MessagePackWriter writer, Dictionary<string, object>? value,
		MessagePackSerializerOptions options)
	{
		if (value == null) return;

		var primitiveFormatter = PrimitiveObjectFormatter.Instance;

		writer.WriteArrayHeader(value.Count);

		foreach (var kvp in value)
		{
			var stringBytes = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(kvp.Key));
			writer.WriteString(in stringBytes);

			primitiveFormatter.Serialize(ref writer, kvp.Value, options);
		}
	}

	public Dictionary<string, object> Deserialize(ref MessagePackReader reader,
		MessagePackSerializerOptions options)
	{
		var output = new Dictionary<string, object>();

		var itemCount = reader.ReadArrayHeader();

		for (var i = 0; i < itemCount; i++)
			output[reader.ReadString()!] =
				PrimitiveObjectFormatter.Instance.Deserialize(ref reader, options)!;

		return output;
	}
}

[MessagePackObject]
public class APIMod
{
	[Key(0)] [JsonProperty("acronym")] public string Acronym { get; set; } = string.Empty;

	[Key(1)]
	[JsonProperty("settings")]
	[MessagePackFormatter(typeof(ModSettingsDictionaryFormatter))]
	public Dictionary<string, object> Settings { get; set; } = new();
}

public enum SpectatedUserState
{
	Idle,
	Playing,
	Paused,
	Passed,
	Failed,
	Quit
}

public enum Ruleset
{
	Osu,
	Mania,
	Tako,
	Catch
}

public enum ScoreRank
{
	F = -1,
	D,
	C,
	B,
	A,
	S,
	SH,
	X,
	XH
}

[AttributeUsage(AttributeTargets.Enum)]
public class HasOrderedElementsAttribute : Attribute
{
	public bool AllowPartialOrdering { get; set; }
}

[AttributeUsage(AttributeTargets.Field)]
public class OrderAttribute(int order) : Attribute
{
	public readonly int Order = order;
}

[HasOrderedElements]
public enum HitResult
{
	[Description("")] [EnumMember(Value = "none")] [Order(15)]
	None,

	[Description("Miss")] [EnumMember(Value = "miss")] [Order(5)]
	Miss,

	[Description("Meh")] [EnumMember(Value = "meh")] [Order(4)]
	Meh,

	[Description("OK")] [EnumMember(Value = "ok")] [Order(3)]
	Ok,

	[Description("Good")] [EnumMember(Value = "good")] [Order(2)]
	Good,

	[Description("Great")] [EnumMember(Value = "great")] [Order(1)]
	Great,

	[Description("Perfect")] [EnumMember(Value = "perfect")] [Order(0)]
	Perfect,

	[EnumMember(Value = "small_tick_miss")] [Order(12)]
	SmallTickMiss,

	[Description("S Tick")] [EnumMember(Value = "small_tick_hit")] [Order(7)]
	SmallTickHit,

	[EnumMember(Value = "large_tick_miss")] [Order(11)]
	LargeTickMiss,

	[Description("L Tick")] [EnumMember(Value = "large_tick_hit")] [Order(6)]
	LargeTickHit,

	[Description("S Bonus")] [EnumMember(Value = "small_bonus")] [Order(10)]
	SmallBonus,

	[Description("L Bonus")] [EnumMember(Value = "large_bonus")] [Order(9)]
	LargeBonus,

	[EnumMember(Value = "ignore_miss")] [Order(14)]
	IgnoreMiss,

	[EnumMember(Value = "ignore_hit")] [Order(13)]
	IgnoreHit,

	[EnumMember(Value = "combo_break")] [Order(16)]
	ComboBreak,

	[EnumMember(Value = "slider_tail_hit")] [Order(8)]
	SliderTailHit,

	[EnumMember(Value = "legacy_combo_increase")] [Order(99)] [Obsolete("Do not use.")]
	LegacyComboIncrease = 99
}

[Serializable]
[MessagePackObject]
public class SpectatorState
{
	[Key(0)] public int? BeatmapID { get; set; }
	[Key(1)] public int? RulesetID { get; set; }
	[Key(2)] public IEnumerable<APIMod> Mods { get; set; } = Enumerable.Empty<APIMod>();
	[Key(3)] public SpectatedUserState State { get; set; }
	[Key(4)] public Dictionary<HitResult, int> MaximumStatistics { get; set; } = new();
}

[MessagePackObject]
public class ScoreProcessorStatistics
{
	[Key(0)] public double BaseScore { get; set; }
	[Key(1)] public double MaximumBaseScore { get; set; }
	[Key(2)] public int AccuracyJudgementCount { get; set; }
	[Key(3)] public double ComboPortion { get; set; }
	[Key(4)] public double BonusPortion { get; set; }
}

[MessagePackObject]
public class FrameHeader
{
	[Key(0)] public long TotalScore { get; set; }
	[Key(1)] public double Accuracy { get; set; }
	[Key(2)] public int Combo { get; set; }
	[Key(3)] public int MaxCombo { get; set; }
	[Key(4)] public Dictionary<HitResult, int> Statistics { get; set; }
	[Key(5)] public ScoreProcessorStatistics ScoreProcessorStatistics { get; set; }
	[Key(6)] public DateTimeOffset ReceivedTime { get; set; }
	[Key(7)] public APIMod[] Mods { get; set; }
	[Key(8)] public long? TotalScoreWithoutMods { get; set; }
	[Key(9)] public int[]? Pauses { get; set; }
}

[Flags]
public enum ReplayButtonState
{
	None = 0,
	Left1 = 1,
	Right1 = 2,
	Left2 = 4,
	Right2 = 8,
	Smoke = 16
}

[MessagePackObject]
public class LegacyReplayFrame
{
	[Key(1)] public float? MouseX;
	[Key(2)] public float? MouseY;
	[Key(3)] public ReplayButtonState ButtonState;
}

[MessagePackObject]
public class FrameDataBundle
{
	[Key(0)] public FrameHeader Header { get; set; }
	[Key(1)] public IList<LegacyReplayFrame> Frames { get; set; }
}

[Serializable]
public class SoloScoreInfo
{
	[JsonProperty("beatmap_id")] public int BeatmapID { get; set; }

	[JsonProperty("ruleset_id")] public int RulesetID { get; set; }

	[JsonProperty("build_id")] public int? BuildID { get; set; }

	[JsonProperty("passed")] public bool Passed { get; set; }

	[JsonProperty("total_score")] public long TotalScore { get; set; }

	[JsonProperty("total_score_without_mods")]
	public long TotalScoreWithoutMods { get; set; }

	[JsonProperty("accuracy")] public double Accuracy { get; set; }

	[JsonProperty("user_id")] public int UserID { get; set; }

	[JsonProperty("max_combo")] public int MaxCombo { get; set; }

	[JsonConverter(typeof(StringEnumConverter))]
	[JsonProperty("rank", DefaultValueHandling = DefaultValueHandling.Include)]
	public ScoreRank Rank { get; set; }

	[JsonProperty("started_at")] public DateTimeOffset? StartedAt { get; set; }

	[JsonProperty("ended_at")] public DateTimeOffset EndedAt { get; set; }

	[JsonProperty("mods")] public APIMod[] Mods { get; set; } = Array.Empty<APIMod>();

	[JsonIgnore]
	[JsonProperty("created_at")]
	public DateTimeOffset CreatedAt { get; set; }

	[JsonIgnore]
	[JsonProperty("updated_at")]
	public DateTimeOffset UpdatedAt { get; set; }

	[JsonIgnore]
	[JsonProperty("deleted_at")]
	public DateTimeOffset? DeletedAt { get; set; }

	[JsonProperty("statistics")] public Dictionary<HitResult, int> Statistics { get; set; } = new();

	[JsonProperty("maximum_statistics")] public Dictionary<HitResult, int> MaximumStatistics { get; set; } = new();

	[JsonProperty("legacy_total_score")] public int? LegacyTotalScore { get; set; }

	[JsonProperty("legacy_score_id")] public ulong? LegacyScoreId { get; set; }

	[JsonProperty("pauses")] public int[] Pauses { get; set; } = [];
}