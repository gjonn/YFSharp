using System.Buffers.Binary;
using System.Text;
using YFSharp.Models;

namespace YFSharp.Internal;

internal static class YahooPricingDataDecoder
{
    public static StreamingPrice Decode(ReadOnlySpan<byte> data)
    {
        var reader = new ProtoReader(data);
        var values = new PricingDataValues();

        while (!reader.End)
        {
            var tag = reader.ReadVarint();
            if (tag == 0)
            {
                throw new FormatException("Invalid protobuf field tag.");
            }

            var fieldNumber = (int)(tag >> 3);
            var wireType = (int)(tag & 0b111);
            switch (fieldNumber)
            {
                case 1:
                    values.Id = reader.ReadString(wireType);
                    break;
                case 2:
                    values.Price = reader.ReadFloat(wireType);
                    break;
                case 3:
                    values.Time = reader.ReadSInt64(wireType);
                    break;
                case 4:
                    values.Currency = reader.ReadString(wireType);
                    break;
                case 5:
                    values.Exchange = reader.ReadString(wireType);
                    break;
                case 6:
                    values.QuoteType = reader.ReadInt32(wireType);
                    break;
                case 7:
                    values.MarketHours = reader.ReadInt32(wireType);
                    break;
                case 8:
                    values.ChangePercent = reader.ReadFloat(wireType);
                    break;
                case 9:
                    values.DayVolume = reader.ReadSInt64(wireType);
                    break;
                case 10:
                    values.DayHigh = reader.ReadFloat(wireType);
                    break;
                case 11:
                    values.DayLow = reader.ReadFloat(wireType);
                    break;
                case 12:
                    values.Change = reader.ReadFloat(wireType);
                    break;
                case 13:
                    values.ShortName = reader.ReadString(wireType);
                    break;
                case 14:
                    values.ExpireDate = reader.ReadSInt64(wireType);
                    break;
                case 15:
                    values.OpenPrice = reader.ReadFloat(wireType);
                    break;
                case 16:
                    values.PreviousClose = reader.ReadFloat(wireType);
                    break;
                case 17:
                    values.StrikePrice = reader.ReadFloat(wireType);
                    break;
                case 18:
                    values.UnderlyingSymbol = reader.ReadString(wireType);
                    break;
                case 19:
                    values.OpenInterest = reader.ReadSInt64(wireType);
                    break;
                case 20:
                    values.OptionsType = reader.ReadSInt64(wireType);
                    break;
                case 21:
                    values.MiniOption = reader.ReadSInt64(wireType);
                    break;
                case 22:
                    values.LastSize = reader.ReadSInt64(wireType);
                    break;
                case 23:
                    values.Bid = reader.ReadFloat(wireType);
                    break;
                case 24:
                    values.BidSize = reader.ReadSInt64(wireType);
                    break;
                case 25:
                    values.Ask = reader.ReadFloat(wireType);
                    break;
                case 26:
                    values.AskSize = reader.ReadSInt64(wireType);
                    break;
                case 27:
                    values.PriceHint = reader.ReadSInt64(wireType);
                    break;
                case 28:
                    values.Volume24Hour = reader.ReadSInt64(wireType);
                    break;
                case 29:
                    values.VolumeAllCurrencies = reader.ReadSInt64(wireType);
                    break;
                case 30:
                    values.FromCurrency = reader.ReadString(wireType);
                    break;
                case 31:
                    values.LastMarket = reader.ReadString(wireType);
                    break;
                case 32:
                    values.CirculatingSupply = reader.ReadDouble(wireType);
                    break;
                case 33:
                    values.MarketCap = reader.ReadDouble(wireType);
                    break;
                default:
                    reader.SkipField(wireType);
                    break;
            }
        }

        return new StreamingPrice
        {
            Id = values.Id ?? string.Empty,
            Price = ToDecimal(values.Price),
            Time = ToUnixTime(values.Time),
            Currency = values.Currency,
            Exchange = values.Exchange,
            QuoteType = values.QuoteType,
            MarketHours = values.MarketHours,
            ChangePercent = ToDecimal(values.ChangePercent),
            DayVolume = values.DayVolume,
            DayHigh = ToDecimal(values.DayHigh),
            DayLow = ToDecimal(values.DayLow),
            Change = ToDecimal(values.Change),
            ShortName = values.ShortName,
            ExpirationDate = ToUnixTime(values.ExpireDate),
            OpenPrice = ToDecimal(values.OpenPrice),
            PreviousClose = ToDecimal(values.PreviousClose),
            StrikePrice = ToDecimal(values.StrikePrice),
            UnderlyingSymbol = values.UnderlyingSymbol,
            OpenInterest = values.OpenInterest,
            OptionsType = values.OptionsType,
            MiniOption = values.MiniOption,
            LastSize = values.LastSize,
            Bid = ToDecimal(values.Bid),
            BidSize = values.BidSize,
            Ask = ToDecimal(values.Ask),
            AskSize = values.AskSize,
            PriceHint = values.PriceHint,
            Volume24Hour = values.Volume24Hour,
            VolumeAllCurrencies = values.VolumeAllCurrencies,
            FromCurrency = values.FromCurrency,
            LastMarket = values.LastMarket,
            CirculatingSupply = values.CirculatingSupply,
            MarketCap = values.MarketCap
        };
    }

    private static decimal? ToDecimal(float? value) =>
        value is null ? null : Convert.ToDecimal(value.Value);

    private static DateTimeOffset? ToUnixTime(long? value)
    {
        if (value is null || value == 0)
        {
            return null;
        }

        return Math.Abs(value.Value) > 10_000_000_000
            ? DateTimeOffset.FromUnixTimeMilliseconds(value.Value)
            : DateTimeOffset.FromUnixTimeSeconds(value.Value);
    }

    private sealed class PricingDataValues
    {
        public string? Id { get; set; }
        public float? Price { get; set; }
        public long? Time { get; set; }
        public string? Currency { get; set; }
        public string? Exchange { get; set; }
        public int? QuoteType { get; set; }
        public int? MarketHours { get; set; }
        public float? ChangePercent { get; set; }
        public long? DayVolume { get; set; }
        public float? DayHigh { get; set; }
        public float? DayLow { get; set; }
        public float? Change { get; set; }
        public string? ShortName { get; set; }
        public long? ExpireDate { get; set; }
        public float? OpenPrice { get; set; }
        public float? PreviousClose { get; set; }
        public float? StrikePrice { get; set; }
        public string? UnderlyingSymbol { get; set; }
        public long? OpenInterest { get; set; }
        public long? OptionsType { get; set; }
        public long? MiniOption { get; set; }
        public long? LastSize { get; set; }
        public float? Bid { get; set; }
        public long? BidSize { get; set; }
        public float? Ask { get; set; }
        public long? AskSize { get; set; }
        public long? PriceHint { get; set; }
        public long? Volume24Hour { get; set; }
        public long? VolumeAllCurrencies { get; set; }
        public string? FromCurrency { get; set; }
        public string? LastMarket { get; set; }
        public double? CirculatingSupply { get; set; }
        public double? MarketCap { get; set; }
    }

    private ref struct ProtoReader
    {
        private readonly ReadOnlySpan<byte> _data;
        private int _position;

        public ProtoReader(ReadOnlySpan<byte> data)
        {
            _data = data;
            _position = 0;
        }

        public readonly bool End => _position >= _data.Length;

        public ulong ReadVarint()
        {
            ulong value = 0;
            var shift = 0;

            while (shift < 64)
            {
                EnsureAvailable(1);
                var current = _data[_position++];
                value |= (ulong)(current & 0x7f) << shift;

                if ((current & 0x80) == 0)
                {
                    return value;
                }

                shift += 7;
            }

            throw new FormatException("Protobuf varint is too long.");
        }

        public string ReadString(int wireType)
        {
            EnsureWireType(wireType, 2);
            var length = checked((int)ReadVarint());
            EnsureAvailable(length);
            var value = Encoding.UTF8.GetString(_data.Slice(_position, length));
            _position += length;
            return value;
        }

        public float ReadFloat(int wireType)
        {
            EnsureWireType(wireType, 5);
            EnsureAvailable(sizeof(float));
            var value = BitConverter.Int32BitsToSingle(
                BinaryPrimitives.ReadInt32LittleEndian(_data.Slice(_position, sizeof(float))));
            _position += sizeof(float);
            return value;
        }

        public double ReadDouble(int wireType)
        {
            EnsureWireType(wireType, 1);
            EnsureAvailable(sizeof(double));
            var value = BitConverter.Int64BitsToDouble(
                BinaryPrimitives.ReadInt64LittleEndian(_data.Slice(_position, sizeof(double))));
            _position += sizeof(double);
            return value;
        }

        public int ReadInt32(int wireType)
        {
            EnsureWireType(wireType, 0);
            return unchecked((int)ReadVarint());
        }

        public long ReadSInt64(int wireType)
        {
            EnsureWireType(wireType, 0);
            var value = ReadVarint();
            return (long)((value >> 1) ^ (ulong)-(long)(value & 1));
        }

        public void SkipField(int wireType)
        {
            switch (wireType)
            {
                case 0:
                    ReadVarint();
                    break;
                case 1:
                    Advance(sizeof(double));
                    break;
                case 2:
                    Advance(checked((int)ReadVarint()));
                    break;
                case 5:
                    Advance(sizeof(float));
                    break;
                default:
                    throw new FormatException($"Unsupported protobuf wire type: {wireType}.");
            }
        }

        private static void EnsureWireType(int actual, int expected)
        {
            if (actual != expected)
            {
                throw new FormatException($"Unexpected protobuf wire type {actual}; expected {expected}.");
            }
        }

        private void Advance(int count)
        {
            EnsureAvailable(count);
            _position += count;
        }

        private readonly void EnsureAvailable(int count)
        {
            if (count < 0 || _position + count > _data.Length)
            {
                throw new FormatException("Protobuf data ended unexpectedly.");
            }
        }
    }
}
