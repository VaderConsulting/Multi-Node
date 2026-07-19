using MultiNodeNetwork.Core.Models;
using MultiNodeNetwork.Core.Protocols;

namespace MultiNodeNetwork.Core.Services;

/// <summary>
/// Interface for master election functionality
/// </summary>
public interface IMasterElection : IDisposable
{
    /// <summary>
    /// Event fired when an election starts
    /// </summary>
    event EventHandler<ElectionStartedEventArgs>? ElectionStarted;

    /// <summary>
    /// Event fired when a master announcement is made
    /// </summary>
    event EventHandler<MasterAnnouncementEventArgs>? MasterAnnouncement;

    /// <summary>
    /// Event fired when a master is elected
    /// </summary>
    event EventHandler<MasterElectedEventArgs>? MasterElected;

    /// <summary>
    /// Get current master ID
    /// </summary>
    string? CurrentMasterId { get; }

    /// <summary>
    /// Get current master node ID
    /// </summary>
    byte CurrentMasterNodeId { get; }

    /// <summary>
    /// Check if this node is the master
    /// </summary>
    bool IsMaster { get; }

    /// <summary>
    /// Check if this node is a backup master
    /// </summary>
    bool IsBackupMaster { get; }

    /// <summary>
    /// Get list of backup masters
    /// </summary>
    IReadOnlyList<byte> BackupMasters { get; }

    /// <summary>
    /// Get master start time
    /// </summary>
    DateTime MasterStartTime { get; }

    /// <summary>
    /// Initialize the master election service
    /// </summary>
    /// <param name="NodeId">Node ID</param>
    /// <param name="NodeIdByte">Node ID as byte</param>
    Task InitializeAsync(string NodeId, byte NodeIdByte);

    /// <summary>
    /// Start an election
    /// </summary>
    Task StartElectionAsync();

    /// <summary>
    /// Start master discovery
    /// </summary>
    Task StartMasterDiscoveryAsync();

    /// <summary>
    /// Handle election message
    /// </summary>
    /// <param name="Message">Election message</param>
    Task HandleElectionMessageAsync(MasterElectionMessage Message);

    /// <summary>
    /// Handle master announcement
    /// </summary>
    /// <param name="Message">Master announcement message</param>
    Task HandleMasterAnnouncementAsync(MasterAnnouncementMessage Message);

    /// <summary>
    /// Handle vote message
    /// </summary>
    /// <param name="Message">Vote message</param>
    Task HandleVoteMessageAsync(ElectionVoteMessage Message);

    /// <summary>
    /// Become master
    /// </summary>
    Task BecomeMasterAsync();

    /// <summary>
    /// Step down as master
    /// </summary>
    Task StepDownAsMasterAsync();

    /// <summary>
    /// Designate backup masters
    /// </summary>
    /// <param name="BackupNodeIds">List of backup node IDs</param>
    void DesignateBackupMasters(List<byte> BackupNodeIds);

    /// <summary>
    /// Add backup master
    /// </summary>
    /// <param name="NodeId">Node ID to add as backup</param>
    void AddBackupMaster(byte NodeId);

    /// <summary>
    /// Remove backup master
    /// </summary>
    /// <param name="NodeId">Node ID to remove from backups</param>
    void RemoveBackupMaster(byte NodeId);
}