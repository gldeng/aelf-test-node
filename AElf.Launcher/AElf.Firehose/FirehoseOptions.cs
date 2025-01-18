namespace AElf.Firehose;

public class FirehoseOptions
{
    public bool WithInitialStateTracking { get; private set; }

    public void EnableInitialStateTracking()
    {
        WithInitialStateTracking = true;
    }
}