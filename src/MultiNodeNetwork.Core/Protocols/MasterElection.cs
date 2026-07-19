using Microsoft.Extensions.Logging;
using MultiNodeNetwork.Common.Models;
using MultiNodeNetwork.Core.Configuration;
using MultiNodeNetwork.Core.Models;
using MultiNodeNetwork.Core.Services;
using System.Collections.Concurrent;

namespace MultiNodeNetwork.Core.Protocols;
/// <summary>
/// Master election protocol implementation
/// </summary>
public class MasterElection : IMasterElection
{
    #region Events

    public event EventHandler<ElectionStartedEventArgs>? ElectionStarted;

    public event EventHandler<MasterAnnouncementEventArgs>? MasterAnnouncement;

    public event EventHandler<MasterElectedEventArgs>? MasterElected;

    #endregion

    #region Fields

    private readonly ConcurrentDictionary<string, ElectionInfo> _ActiveElections = new();
    private readonly Communication _Communication;
    private readonly NetworkConfig _Config;
    private string? _CurrentMasterId;
    private byte _CurrentMasterNodeId = 0;
    private bool _Disposed = false;
    private readonly Timer _ElectionTimeoutTimer;
    private readonly ConcurrentDictionary<string, List<ElectionVote>> _ElectionVotes = new();
    private bool _IsMaster = false;
    private readonly ILogger<MasterElection> _Logger;
    private readonly Timer _MasterBroadcastTimer;
    private DateTime _MasterStartTime = DateTime.UtcNow;
    private string? _MyNodeId;
    private byte _MyNodeIdByte = 0;
    private readonly Timer _MasterFailureDetectionTimer;
    private DateTime _LastMasterAnnouncement = DateTime.UtcNow;
    private readonly List<byte> _BackupMasters = [];
    private bool _IsBackupMaster = false;

    #endregion

    #region Properties

    /// <summary>
    /// Get current master ID
    /// </summary>
    public string? CurrentMasterId
    {
        get
        {
            return _CurrentMasterId;
        }
    }

    /// <summary>
    /// Get current master node ID
    /// </summary>
    public byte CurrentMasterNodeId
    {
        get
        {
            return _CurrentMasterNodeId;
        }
    }

    /// <summary>
    /// Check if this node is the master
    /// </summary>
    public bool IsMaster
    {
        get
        {
            return _IsMaster;
        }
    }

    /// <summary>
    /// Check if this node is a backup master
    /// </summary>
    public bool IsBackupMaster
    {
        get
        {
            return _IsBackupMaster;
        }
    }

    /// <summary>
    /// Get list of backup masters
    /// </summary>
    public IReadOnlyList<byte> BackupMasters
    {
        get
        {
            return _BackupMasters.AsReadOnly();
        }
    }

    /// <summary>
    /// Get master start time
    /// </summary>
    public DateTime MasterStartTime
    {
        get
        {
            return _MasterStartTime;
        }
    }

    #endregion

    #region Constructors

    public MasterElection(ILogger<MasterElection> Logger, NetworkConfig Config, Communication Communication)
    {
        _Logger = Logger;
        _Config = Config;
        _Communication = Communication;
        _ElectionTimeoutTimer = new Timer(CheckElectionTimeouts, null, Timeout.Infinite, Timeout.Infinite);
        _MasterBroadcastTimer = new Timer(BroadcastMasterPresence, null, Timeout.Infinite, Timeout.Infinite);
        _MasterFailureDetectionTimer = new Timer(CheckMasterFailure, null, Timeout.Infinite, Timeout.Infinite);
    }

    #endregion

    #region Private Methods

    private async Task BroadcastElectionMessageAsync(MasterElectionMessage Message)
    {
        try
        {
            bool Success = await _Communication.BroadcastMessageAsync(Message);
            if (Success)
            {
                _Logger.LogDebug("Successfully broadcasted election message: {MessageId}", Message.MessageId);
            }
            else
            {
                _Logger.LogWarning("Failed to broadcast election message: {MessageId}", Message.MessageId);
            }
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Error broadcasting election message: {MessageId}", Message.MessageId);
        }
    }

    private async Task BroadcastElectionResultAsync(ElectionResultMessage Message)
    {
        try
        {
            bool Success = await _Communication.BroadcastMessageAsync(Message);
            if (Success)
            {
                _Logger.LogDebug("Successfully broadcasted election result: {MessageId}", Message.MessageId);
            }
            else
            {
                _Logger.LogWarning("Failed to broadcast election result: {MessageId}", Message.MessageId);
            }
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Error broadcasting election result: {MessageId}", Message.MessageId);
        }
    }

    private async Task BroadcastMasterAnnouncementAsync(MasterAnnouncementMessage Message)
    {
        try
        {
            bool Success = await _Communication.BroadcastMessageAsync(Message);
            if (Success)
            {
                _Logger.LogDebug("Successfully broadcasted master announcement: {MessageId}", Message.MessageId);
            }
            else
            {
                _Logger.LogWarning("Failed to broadcast master announcement: {MessageId}", Message.MessageId);
            }
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Error broadcasting master announcement: {MessageId}", Message.MessageId);
        }
    }

    private async void BroadcastMasterPresence(object? State)
    {
        if (_Disposed || !_IsMaster)
        {
            return;
        }

        try
        {
            MasterAnnouncementMessage Announcement = new MasterAnnouncementMessage
            {
                SenderId = _MyNodeId!,
                SenderNodeId = _MyNodeIdByte,
                NetworkInfo = new Dictionary<string, object>
                {
                    ["masterId"] = _MyNodeId!,
                    ["masterNodeId"] = _MyNodeIdByte,
                    ["startTime"] = _MasterStartTime,
                    ["uptime"] = DateTime.UtcNow - _MasterStartTime,
                    ["backupMasters"] = _BackupMasters
                },
                NodeCount = 0, // Will be updated by topology manager
                TopologyVersion = 0,
                MasterStartTime = _MasterStartTime
            };

            await BroadcastMasterAnnouncementAsync(Announcement);
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Error broadcasting master presence");
        }
    }

    private async Task BroadcastVoteMessageAsync(ElectionVoteMessage Message)
    {
        try
        {
            bool Success = await _Communication.BroadcastMessageAsync(Message);
            if (Success)
            {
                _Logger.LogDebug("Successfully broadcasted vote message: {MessageId}", Message.MessageId);
            }
            else
            {
                _Logger.LogWarning("Failed to broadcast vote message: {MessageId}", Message.MessageId);
            }
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Error broadcasting vote message: {MessageId}", Message.MessageId);
        }
    }

    private int CalculateNodePriority(byte NodeId)
    {
        NodeType NodeType = NodeIdAllocation.GetNodeType(NodeId);
        int Priority = 0;

        // Base priority by node type
        switch (NodeType)
        {
            case NodeType.Master:
                Priority += 1000;
                break;
            case NodeType.Repeater:
                Priority += 500;
                break;
            case NodeType.UserDevice:
                Priority += 100;
                break;
        }

        // Add priority based on node ID (lower IDs get higher priority for same type)
        Priority += 255 - NodeId;

        return Priority;
    }

    private int CalculatePriority()
    {
        NodeType NodeType = NodeIdAllocation.GetNodeType(_MyNodeIdByte);
        int Priority = 0;

        // Base priority by node type
        switch (NodeType)
        {
            case NodeType.Master:
                Priority += 1000;
                break;
            case NodeType.Repeater:
                Priority += 500;
                break;
            case NodeType.UserDevice:
                Priority += 100;
                break;
        }

        // Add priority based on node ID (lower IDs get higher priority for same type)
        Priority += 255 - _MyNodeIdByte;

        // Add priority based on signal strength (if available)
        Priority += 25;

        // Add priority based on uptime
        Priority += 25;

        return Priority;
    }

    private async void CheckElectionTimeouts(object? State)
    {
        if (_Disposed)
        {
            return;
        }

        DateTime Now = DateTime.UtcNow;
        List<string> TimedOutElections = _ActiveElections
            .Where(kvp => Now - kvp.Value.StartTime > TimeSpan.FromSeconds(_Config.Timing.ElectionTimeout))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (string ElectionId in TimedOutElections)
        {
            _Logger.LogWarning("Election {ElectionId} timed out", ElectionId);
            await ConcludeElectionAsync(ElectionId);
        }
    }

    private async Task ConcludeElectionAsync(string ElectionId)
    {
        if (!_ElectionVotes.TryGetValue(ElectionId, out List<ElectionVote>? Votes))
        {
            return;
        }

        // Count votes for each candidate
        Dictionary<byte, int> VoteCounts = Votes
            .Where(v => v.Vote)
            .GroupBy(v => v.CandidateNodeId)
            .ToDictionary(g => g.Key, g => g.Count());

        if (VoteCounts.Any())
        {
            byte Winner = VoteCounts.OrderByDescending(kvp => kvp.Value).First().Key;
            string WinnerId = Votes.First(v => v.CandidateNodeId == Winner).CandidateId;

            _Logger.LogInformation("Election {ElectionId} concluded. Winner: {WinnerId} (0x{Winner:X2}) with {VoteCount} votes",
                ElectionId, WinnerId, Winner, VoteCounts[Winner]);

            // Broadcast election result
            ElectionResultMessage ResultMessage = new ElectionResultMessage
            {
                SenderId = _MyNodeId!,
                SenderNodeId = _MyNodeIdByte,
                ElectionId = ElectionId,
                WinnerId = WinnerId,
                WinnerNodeId = Winner,
                VoteCount = VoteCounts[Winner],
                TotalVotes = Votes.Count
            };

            await BroadcastElectionResultAsync(ResultMessage);

            // If we won, become master
            if (Winner == _MyNodeIdByte)
            {
                await BecomeMasterAsync();
            }
        }

        // Clean up election
        _ActiveElections.TryRemove(ElectionId, out _);
        _ElectionVotes.TryRemove(ElectionId, out _);
    }

    private byte DetermineBestCandidate(List<byte> Candidates)
    {
        // Priority-based selection considering node type and capabilities
        return Candidates.OrderByDescending(c => CalculateNodePriority(c)).First();
    }

    private Dictionary<string, object> GetCapabilities()
    {
        NodeType NodeType = NodeIdAllocation.GetNodeType(_MyNodeIdByte);

        Dictionary<string, object> Capabilities = new Dictionary<string, object>
        {
            ["nodeType"] = NodeType.ToString(),
            ["signalStrength"] = 0, // Will be updated by WiFi manager
            ["batteryLevel"] = 100,
            ["version"] = "1.0.0"
        };

        // Add type-specific capabilities
        switch (NodeType)
        {
            case NodeType.Master:
                Capabilities["canBeMaster"] = true;
                Capabilities["canRoute"] = true;
                Capabilities["maxConnections"] = 50;
                break;
            case NodeType.Repeater:
                Capabilities["canBeMaster"] = false;
                Capabilities["canRoute"] = true;
                Capabilities["maxConnections"] = 10;
                break;
            case NodeType.UserDevice:
                Capabilities["canBeMaster"] = false;
                Capabilities["canRoute"] = false;
                Capabilities["maxConnections"] = 1;
                break;
        }

        return Capabilities;
    }

    private async void CheckMasterFailure(object? State)
    {
        if (_Disposed || _IsMaster)
        {
            return;
        }

        DateTime Now = DateTime.UtcNow;
        TimeSpan TimeSinceLastAnnouncement = Now - _LastMasterAnnouncement;

        if (TimeSinceLastAnnouncement > TimeSpan.FromSeconds(_Config.Timing.MasterTimeout))
        {
            _Logger.LogWarning("Master failure detected. No announcement for {Timeout} seconds", _Config.Timing.MasterTimeout);

            // Check if we are a backup master
            if (_IsBackupMaster)
            {
                _Logger.LogInformation("Backup master {NodeId} (0x{NodeIdByte:X2}) taking over as primary master",
                    _MyNodeId, _MyNodeIdByte);
                await BecomeMasterAsync();
            }
            else if (_BackupMasters.Count == 0)
            {
                // No backup masters available, start election with lowest ID priority
                _Logger.LogInformation("No backup masters available. Starting election with lowest ID priority");
                await StartElectionWithLowestIdPriorityAsync();
            }
            else
            {
                // Check if any backup masters are available
                _Logger.LogInformation("Backup masters available: {BackupCount}. Waiting for backup takeover",
                    _BackupMasters.Count);
            }
        }
    }

    /// <summary>
    /// Start election with lowest ID priority when no backup masters are available
    /// </summary>
    private async Task StartElectionWithLowestIdPriorityAsync()
    {
        if (_Disposed)
        {
            return;
        }

        string ElectionId = Guid.NewGuid().ToString();
        _Logger.LogInformation("Starting lowest ID priority election {ElectionId}", ElectionId);

        ElectionInfo ElectionInfo = new ElectionInfo
        {
            ElectionId = ElectionId,
            StartTime = DateTime.UtcNow,
            Candidates = [_MyNodeIdByte],
            Status = ElectionStatus.InProgress
        };

        _ActiveElections[ElectionId] = ElectionInfo;
        _ElectionVotes[ElectionId] = [];

        // Start election timeout
        _ElectionTimeoutTimer.Change(
            (int)TimeSpan.FromSeconds(_Config.Timing.ElectionTimeout).TotalMilliseconds,
            Timeout.Infinite);

        ElectionStarted?.Invoke(this, new ElectionStartedEventArgs(ElectionId));

        // Broadcast election message with lowest ID priority
        MasterElectionMessage ElectionMessage = new MasterElectionMessage
        {
            SenderId = _MyNodeId!,
            SenderNodeId = _MyNodeIdByte,
            ElectionId = ElectionId,
            CandidateInfo = new Dictionary<string, object>
            {
                ["nodeId"] = _MyNodeId!,
                ["nodeIdByte"] = _MyNodeIdByte,
                ["priority"] = CalculateLowestIdPriority(),
                ["capabilities"] = GetCapabilities(),
                ["electionType"] = "lowest_id_failover"
            },
            Priority = CalculateLowestIdPriority()
        };

        await BroadcastElectionMessageAsync(ElectionMessage);
    }

    /// <summary>
    /// Calculate priority for lowest ID failover election
    /// </summary>
    private int CalculateLowestIdPriority()
    {
        // For lowest ID failover, lower node IDs have higher priority
        return 255 - _MyNodeIdByte;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Become the master node
    /// </summary>
    public Task BecomeMasterAsync()
    {
        if (_Disposed || _IsMaster)
        {
            return Task.CompletedTask;
        }

        _IsMaster = true;
        _CurrentMasterId = _MyNodeId;
        _CurrentMasterNodeId = _MyNodeIdByte;
        _MasterStartTime = DateTime.UtcNow;

        _Logger.LogInformation("Node {NodeId} (0x{NodeIdByte:X2}) became master", _MyNodeId, _MyNodeIdByte);

        // Start broadcasting master presence
        _MasterBroadcastTimer.Change(
            TimeSpan.Zero,
            TimeSpan.FromSeconds(_Config.Timing.MasterBroadcastInterval));

        MasterElected?.Invoke(this, new MasterElectedEventArgs(_MyNodeId!, _MyNodeIdByte));

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_Disposed)
        {
            return;
        }

        _Disposed = true;
        _ElectionTimeoutTimer?.Dispose();
        _MasterBroadcastTimer?.Dispose();
        _MasterFailureDetectionTimer?.Dispose();
    }

    /// <summary>
    /// Handle incoming election message
    /// </summary>
    public async Task HandleElectionMessageAsync(MasterElectionMessage Message)
    {
        if (_Disposed)
        {
            return;
        }

        _Logger.LogDebug("Received election message from {SenderId} (0x{SenderNodeId:X2}) for election {ElectionId}",
            Message.SenderId, Message.SenderNodeId, Message.ElectionId);

        // Add candidate to election if not already present
        if (_ActiveElections.TryGetValue(Message.ElectionId, out ElectionInfo? ElectionInfo))
        {
            if (!ElectionInfo.Candidates.Contains(Message.SenderNodeId))
            {
                ElectionInfo.Candidates.Add(Message.SenderNodeId);
            }
        }
        else
        {
            // New election, join it
            ElectionInfo = new ElectionInfo
            {
                ElectionId = Message.ElectionId,
                StartTime = DateTime.UtcNow,
                Candidates = [Message.SenderNodeId, _MyNodeIdByte],
                Status = ElectionStatus.InProgress
            };
            _ActiveElections[Message.ElectionId] = ElectionInfo;
            _ElectionVotes[Message.ElectionId] = [];

            // Start election timeout
            _ElectionTimeoutTimer.Change(
                (int)TimeSpan.FromSeconds(_Config.Timing.ElectionTimeout).TotalMilliseconds,
                Timeout.Infinite);
        }

        // Vote for the best candidate
        byte BestCandidate = DetermineBestCandidate(ElectionInfo.Candidates);
        ElectionVote Vote = new ElectionVote
        {
            ElectionId = Message.ElectionId,
            VoterId = _MyNodeId!,
            VoterNodeId = _MyNodeIdByte,
            CandidateId = Message.SenderId,
            CandidateNodeId = BestCandidate,
            Vote = true,
            Timestamp = DateTime.UtcNow
        };

        _ElectionVotes[Message.ElectionId].Add(Vote);

        // Broadcast vote
        ElectionVoteMessage VoteMessage = new ElectionVoteMessage
        {
            SenderId = _MyNodeId!,
            SenderNodeId = _MyNodeIdByte,
            ElectionId = Message.ElectionId,
            CandidateId = Message.SenderId,
            CandidateNodeId = BestCandidate,
            Vote = true
        };

        await BroadcastVoteMessageAsync(VoteMessage);
    }

    /// <summary>
    /// Handle master announcement message
    /// </summary>
    public async Task HandleMasterAnnouncementAsync(MasterAnnouncementMessage Message)
    {
        if (_Disposed)
        {
            return;
        }

        _Logger.LogInformation("Received master announcement from {SenderId} (0x{SenderNodeId:X2})",
            Message.SenderId, Message.SenderNodeId);

        _CurrentMasterId = Message.SenderId;
        _CurrentMasterNodeId = Message.SenderNodeId;
        _LastMasterAnnouncement = DateTime.UtcNow;

        // Update backup masters list if provided
        if (Message.NetworkInfo.TryGetValue("backupMasters", out object? BackupMastersObj) &&
            BackupMastersObj is List<byte> BackupMastersList)
        {
            _BackupMasters.Clear();
            _BackupMasters.AddRange(BackupMastersList);
            _Logger.LogDebug("Updated backup masters list: {BackupMasters}",
                string.Join(", ", _BackupMasters.Select(b => $"0x{b:X2}")));
        }

        // Check if we are a backup master
        _IsBackupMaster = _BackupMasters.Contains(_MyNodeIdByte);
        if (_IsBackupMaster)
        {
            _Logger.LogInformation("Node {NodeId} (0x{NodeIdByte:X2}) is designated as backup master",
                _MyNodeId, _MyNodeIdByte);
        }

        // If we were the master, step down
        if (_IsMaster && Message.SenderNodeId != _MyNodeIdByte)
        {
            await StepDownAsMasterAsync();
        }

        // Start master failure detection timer
        _MasterFailureDetectionTimer.Change(
            (int)TimeSpan.FromSeconds(_Config.Timing.MasterTimeout).TotalMilliseconds,
            (int)TimeSpan.FromSeconds(_Config.Timing.MasterTimeout).TotalMilliseconds);

        MasterAnnouncement?.Invoke(this, new MasterAnnouncementEventArgs(Message.SenderId, Message.SenderNodeId));
    }

    /// <summary>
    /// Handle incoming vote message
    /// </summary>
    public async Task HandleVoteMessageAsync(ElectionVoteMessage Message)
    {
        if (_Disposed)
        {
            return;
        }

        _Logger.LogDebug("Received vote from {SenderId} (0x{SenderNodeId:X2}) for {CandidateId} (0x{CandidateNodeId:X2}) in election {ElectionId}",
            Message.SenderId, Message.SenderNodeId, Message.CandidateId, Message.CandidateNodeId, Message.ElectionId);

        if (_ElectionVotes.TryGetValue(Message.ElectionId, out List<ElectionVote>? Votes))
        {
            Votes.Add(new ElectionVote
            {
                ElectionId = Message.ElectionId,
                VoterId = Message.SenderId,
                VoterNodeId = Message.SenderNodeId,
                CandidateId = Message.CandidateId,
                CandidateNodeId = Message.CandidateNodeId,
                Vote = Message.Vote,
                Timestamp = DateTime.UtcNow
            });

            // Check if we have enough votes to conclude the election
            if (Votes.Count >= _ActiveElections[Message.ElectionId].Candidates.Count)
            {
                await ConcludeElectionAsync(Message.ElectionId);
            }
        }
    }

    /// <summary>
    /// Initialize the master election protocol
    /// </summary>
    public async Task InitializeAsync(string NodeId, byte NodeIdByte)
    {
        _MyNodeId = NodeId;
        _MyNodeIdByte = NodeIdByte;
        _Logger.LogInformation("Initializing master election protocol for node {NodeId} (0x{NodeIdByte:X2})",
            NodeId, NodeIdByte);

        // Start listening for master announcements
        await StartMasterDiscoveryAsync();
    }

    /// <summary>
    /// Start a new master election
    /// </summary>
    public async Task StartElectionAsync()
    {
        if (_Disposed)
        {
            return;
        }

        string ElectionId = Guid.NewGuid().ToString();
        _Logger.LogInformation("Starting master election {ElectionId}", ElectionId);

        ElectionInfo ElectionInfo = new ElectionInfo
        {
            ElectionId = ElectionId,
            StartTime = DateTime.UtcNow,
            Candidates = [_MyNodeIdByte],
            Status = ElectionStatus.InProgress
        };

        _ActiveElections[ElectionId] = ElectionInfo;
        _ElectionVotes[ElectionId] = [];

        // Start election timeout
        _ElectionTimeoutTimer.Change(
            (int)TimeSpan.FromSeconds(_Config.Timing.ElectionTimeout).TotalMilliseconds,
            Timeout.Infinite);

        ElectionStarted?.Invoke(this, new ElectionStartedEventArgs(ElectionId));

        // Broadcast election message
        MasterElectionMessage ElectionMessage = new MasterElectionMessage
        {
            SenderId = _MyNodeId!,
            SenderNodeId = _MyNodeIdByte,
            ElectionId = ElectionId,
            CandidateInfo = new Dictionary<string, object>
            {
                ["nodeId"] = _MyNodeId!,
                ["nodeIdByte"] = _MyNodeIdByte,
                ["priority"] = CalculatePriority(),
                ["capabilities"] = GetCapabilities()
            },
            Priority = CalculatePriority()
        };

        await BroadcastElectionMessageAsync(ElectionMessage);
    }

    /// <summary>
    /// Start the master discovery process
    /// </summary>
    public async Task StartMasterDiscoveryAsync()
    {
        _Logger.LogInformation("Starting master discovery process");

        // Wait for a short period to see if there's already a master
        await Task.Delay(TimeSpan.FromSeconds(2));

        // If no master found, start election
        if (string.IsNullOrEmpty(_CurrentMasterId))
        {
            await StartElectionAsync();
        }
    }

    /// <summary>
    /// Step down as master
    /// </summary>
    public Task StepDownAsMasterAsync()
    {
        if (_Disposed || !_IsMaster)
        {
            return Task.CompletedTask;
        }

        _IsMaster = false;
        _MasterBroadcastTimer.Change(Timeout.Infinite, Timeout.Infinite);

        _Logger.LogInformation("Node {NodeId} (0x{NodeIdByte:X2}) stepped down as master", _MyNodeId, _MyNodeIdByte);
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Designate backup masters for failover
    /// </summary>
    public void DesignateBackupMasters(List<byte> BackupNodeIds)
    {
        _BackupMasters.Clear();
        _BackupMasters.AddRange(BackupNodeIds);

        _Logger.LogInformation("Designated backup masters: {BackupMasters}",
            string.Join(", ", _BackupMasters.Select(b => $"0x{b:X2}")));

        // Check if we are now a backup master
        _IsBackupMaster = _BackupMasters.Contains(_MyNodeIdByte);
        if (_IsBackupMaster)
        {
            _Logger.LogInformation("Node {NodeId} (0x{NodeIdByte:X2}) is now a backup master",
                _MyNodeId, _MyNodeIdByte);
        }
    }

    /// <summary>
    /// Add a single backup master
    /// </summary>
    public void AddBackupMaster(byte NodeId)
    {
        if (!_BackupMasters.Contains(NodeId))
        {
            _BackupMasters.Add(NodeId);
            _Logger.LogInformation("Added backup master: 0x{NodeId:X2}", NodeId);

            // Check if we are now a backup master
            _IsBackupMaster = _BackupMasters.Contains(_MyNodeIdByte);
            if (_IsBackupMaster)
            {
                _Logger.LogInformation("Node {NodeId} (0x{NodeIdByte:X2}) is now a backup master",
                    _MyNodeId, _MyNodeIdByte);
            }
        }
    }

    /// <summary>
    /// Remove a backup master
    /// </summary>
    public void RemoveBackupMaster(byte NodeId)
    {
        if (_BackupMasters.Remove(NodeId))
        {
            _Logger.LogInformation("Removed backup master: 0x{NodeId:X2}", NodeId);

            // Check if we are no longer a backup master
            _IsBackupMaster = _BackupMasters.Contains(_MyNodeIdByte);
            if (!_IsBackupMaster)
            {
                _Logger.LogInformation("Node {NodeId} (0x{NodeIdByte:X2}) is no longer a backup master",
                    _MyNodeId, _MyNodeIdByte);
            }
        }
    }

    #endregion
}
