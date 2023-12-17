namespace XProtocol;

public class XPacketField
{
    public byte FieldId { get; init; }
    public byte FieldSize { get; set; }
    public byte[]? Contents { get; set; }
}