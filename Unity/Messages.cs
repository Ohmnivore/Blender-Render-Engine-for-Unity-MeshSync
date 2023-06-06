namespace BlenderBridge
{
    public enum ServerMessageType : byte
    {
        Heartbeat = 0,
        DomainReload = 1
    }

    public enum ClientMessageType : byte
    {
        ViewUpdated = 0,
        ViewDestroyed = 1,
        ObjectVisibility = 2,
    }

    struct DomainReload : Comms.IMessage
    {
        public byte Type => (byte)ServerMessageType.DomainReload;
    }

    struct Heartbeat : Comms.IMessage
    {
        public byte Type => (byte)ServerMessageType.Heartbeat;
    }
}
