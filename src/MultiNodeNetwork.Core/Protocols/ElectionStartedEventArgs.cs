namespace MultiNodeNetwork.Core.Protocols;
/// <summary>
/// Event arguments for when an election starts
/// </summary>
public class ElectionStartedEventArgs : EventArgs
{
#region Properties

    public string ElectionId { get; }

#endregion

#region Constructors

    public ElectionStartedEventArgs(string electionId) => ElectionId = electionId;

#endregion
}
