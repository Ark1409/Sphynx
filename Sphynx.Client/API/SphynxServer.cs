namespace Sphynx.Client.API;

public abstract class SphynxServer
{
    protected SphynxPacketRouter Router { get; }

    public SphynxServer(SphynxPacketRouter router)
    {
        Router = router;
    }
}
