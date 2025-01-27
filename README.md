## ACTUAL LOG REPLICATION SCENARIOS

1. (DONE) When a leader receives a client command, the leader sends the log entry in the next AppendEntries RPC to all nodes.  
2. (DONE) When a leader receives a command from the client, it is appended to its log.  
3. (DONE) When a node is new, its log is empty.  
4. (DONE) When a leader wins an election, it initializes the nextIndex for each follower to the index just after the last one in its log.  
5. (DONE) Leaders maintain a "nextIndex" for each follower that is the index of the next log entry the leader will send to that follower.  
6. (DONE) The highest committed index from the leader is included in AppendEntries RPCs.  
7. When a follower learns that a log entry is committed, it applies the entry to its local state machine.  
8. When the leader has received a majority confirmation of a log, it commits it.  
9. (DONE) The leader commits logs by incrementing its committed log index.  
10. (DONE) Given a follower receives an AppendEntries with logs, it will add those entries to its personal log.  
11. (DONE) A follower's response to an AppendEntries includes the follower's term number and if it was sucessfulOrNot
12. (DONE) When a leader receives majority responses from the clients after a log replication heartbeat, the leader sends a confirmation response to the client.  
13. (DONE) Given a leader node, when a log is committed, it applies it to its internal state machine.  
14. when a follower receives a valid heartbeat, it increases its commitIndex to match the commit index of the heartbeat
    1. reject the heartbeat if the previous log index / term number does not match your log 
15. When sending an AppendEntries RPC, the leader includes the index and term of the entry in its log that immediately precedes the new entries.  
    1. If the follower does not find an entry in its log with the same index and term, then it refuses the new entries.  
        1. Term must be the same or newer.  
        2. If the index is greater, it will be decreased by the leader.  
        3. If the index is less, the follower deletes what it has.  
    2. If a follower rejects the AppendEntries RPC, the leader decrements nextIndex and retries the AppendEntries RPC.  
16. (DONE) When a leader sends a heartbeat with a log but does not receive responses from a majority of nodes, the entry is uncommitted.  
17. (DONE) If a leader does not receive a response from a follower, the leader continues to send the log entries in subsequent heartbeats.  
18. (DONE) If a leader cannot commit an entry, it does not send a response to the client.  
19. If a node receives an AppendEntries with logs that are too far in the future from its local state, it should reject the AppendEntries.  
20. If a node receives an AppendEntries with a term and index that do not match, it will reject the AppendEntries until it finds a matching log.  

## Log Replication Test Scenarios

1. Given a leader has appended an entry to its log and replicated to majority of servers
When the leader goes to commit it, but breaks down
Then the new leader should not overwrite the old uncommited entry

2. Given the leader has appended a new entry to its log
When the operation has been completed
Then it should send AppendEntriesRPCs with the new entry in parallel to the other nodes

3. Given the leader has sent an AppendEntriesRPC with a new log entry to the other nodes
When the node it has sent it to is unresponsive
Then the leader should be sending ApeendEntriesRPC indefenetely

4. Given the leader has sent an AppendEntriesRPC with a new log entry
When the entry has been replicated on a majority of the severs
Then the leader should commit it (I'm not sure what is meant by commit)

5. Given the leader has committed a log entry,
When the operation is complete,
Then the leader should update its highest index commited

6. Given the leader has updated its highest index commited
When it goes to send a heartbeat to the other nodes
Then the highest index commited should be included in the AppendEntriesRPC

7. Given that the leader has sent an AppendEntriesRPC to the other nodes
When a node realizes that a new log entry has been commited
Then it should save that log entry in its log

8. Given a leader sent an AppendEntriesRPC with the index and the term of the new log entry
When a follower doesn't find a an entry in its log with the same index and term
Then the follower should refuse any new entries and return failure

9. Given a leader sent an AppendEntriesRPC with the index and the term of the new log entry
When a follower does find a an entry in its log with the same index and term
Then the follower should return success

10. Given a leader has been created
When it's being initialized
Then it should initialize all the nextIndex values to the index immediately following the last entry in its log

11. Given the leader sent an AppendEntriesRPC with the index and the term
When a follower detects an inconsistency and returns failure
Then the leader should decrement it's nextIndex value and retry the AppendEntriesRPC

12. Given a candidate sends RequestVoteRPCs (including information about its log) to all other nodes,
When a node determines that its own log is more up-to-date than the candidate's,
Then it should respond with a RespondVoteRequestRPC rejecting the vote request.

## Considerations:

1. Ensure all data mutations (e.g., logs, indices, terms) are thread-safe.  

2. Persist critical state (e.g., logs, current term) to disk before responding to RPCs and to maintain consistency after restarts  (Maybe)

3. Validate log consistency before appending entries to avoid corruption.  

4. Ensure `AppendEntriesRPC` retries stop only after the follower responds or the leader steps down.  

5. Gracefully handle network partitions and ensure majority consensus before committing entries.  

6. Save committed entries to disk as soon as they are replicated on a majority of nodes.  (Maybe)

7. Prevent split-brain by ensure only one leader exists in a given term.  

8. Protect against race conditions during leader initialization and election.  

9. Handle unresponsive nodes without delaying progress on the majority of responsive nodes.  

## Actual Scenarios:

1.(DONE) When a leader is active it sends a heart beat within 50ms.  

2.(DONE) When a node receives an AppendEntries from another node, then first node remembers that other node is the current leader.  

3.(DONE) When a new node is initialized, it should be in follower state.

4.(DONE) When a follower doesn't get a message for 300ms then it starts an election.  

5.(DONE) When the election time is reset, it is a random value between 150 and 300ms.  
   - Between  
   - Random: call n times and make sure that there are some that are different (otherproperties of the distribution if you like) 

6.(DONE)When a new election begins, the term is incremented by 1.  
   - Create a new node, store id in variable.  
   - Wait 300 ms  
   - Reread term (?)  
   - Assert after is greater (by at least 1)  

7.(DONE) When a follower does get an AppendEntries message, it resets the election timer. (i.e. it doesn't start an election even after more than 300ms)  

8.(DONE) Given an election begins, when the candidate gets a majority of votes, it becomes a leader. (think of the easy case; can use two tests for single and multi-node clusters)  

9. (DONE) Given a candidate receives a majority of votes while waiting for unresponsive node, it still becomes a leader.  

10. (DONE) A follower that has not voted and is in an earlier term responds to a RequestForVoteRPC with yes. (the reply will be a separate RPC) 

11. (DONE) Given a candidate server that just became a candidate, it votes for itself.  

12.(DONE) Given a candidate, when it receives an AppendEntries message from a node with a later term, then candidate loses and becomes a follower. 

13. (DONE) Given a candidate, when it receives an AppendEntries message from a node with an equal term, then candidate loses and becomes a follower.

14. (DONE) If a node receives a second request for vote for the same term, it should respond no. (again, separate RPC for response)  

15. (DONE) If a node receives a second request for vote for a future term, it should vote for that node.  

16. (DONE) Given a candidate, when an election timer expires inside of an election, a new election is started.  

17.(DONE)  When a follower node receives an AppendEntries request, it sends a response.  

18.(DONE) Given a candidate receives an AppendEntries from a previous term, then rejects.  

19. (DONE) When a candidate wins an election, it immediately sends a heart beat.  

20.(Testing persistence to disk will be a later assignment.)  

## Scenarios:

1. Given a cluster with 3 uninitilaized servers 
When the servers are initialized and start running
Then they should all be in follower state by default

2. Given a cluster with 3 follower servers
When the election timeout has passed
Then one of the servers should increment its current term and transition to candidate state

3. Given a cluster with 3 servers, 2 followers and 1 candidate
When the candidate is initialized and votes for itself
Then it should send RequestVoteRPCs to the follower servers

4. Given a cluster with 3 servers, 2 followers and 1 candidate
When the candidate has received votes from the 2 follower servers
Then it should transition to leader state

5. Given a cluster with 3 servers, 2 followers and 1 leader
When the leader is initialized
Then it should send empty AppendEntriesRPCs (heartbeats) to the follower servers

6. Given a cluster with 3 servers, 1 follower, 1 candidate and 1 leader
When the candidate server receives a AppendEntriesRPC from the leader with the same term
Then it should transition back to the follower state

7. Given a cluster with 3 servers, 1 follower, 1 candidate and 1 leader
When the candidate server receives a AppendEntriesRPC from the leader with a higher term
Then it should transition back to the follower state

8. Given a cluster with 3 servers, 1 follower, 1 candidate and 1 leader
When the candidate server receives a AppendEntriesRPC from the leader with a lower term
Then it should stay in the candidate state

9. Given a cluster with 4 servers, 2 followers and 2 candidates with the same randomized timoeouts
When both candidates fail to receive a majority of votes
Then both should restart their election process with new randomized timeouts

10. Given a cluster with 6 servers, 4 followers and 2 candidates with the same randomized timeouts
When both candidates fail to receive a majority of votes
Then both should restart their election process with new randomized timeouts.

11. Given a cluster with 4 servers, 2 followers and 2 candidates with different randomized timeouts
When both candidates send their RequestVoteRPCs
Then the candidate with the lower randomized timeout should get the majority of the votes and be elected leader

12. Given a cluster with 2 servers, both of them in candidate state, with different terms
When one receives a RequestVoteRPC from the other candidate with a higher term
Then it should transition back to follower state

13. Given a cluster with 1 uninitilaized servers
When the server is initialized and starts running
Then it should be in follower state by default

14. Given a cluster with 4 follower servers
When the election timeout has passed
Then at least one of the servers should increment its current term and transition to candidate state.

15. Given a cluster with 2 uninitialized servers
When the servers are initialized and start running
Then they should both be in follower state by default.

16. Given a cluster with 6 servers, 5 followers and 1 candidate
When the one candidate receives the votes
Then it should transition to leader state and immediately send AppendEntriesRPCs (heartbeats) to all follower servers.

17. Given a cluster with 5 servers, 3 followers and 2 candidates with different randomized timeouts
When the one candidate receives votes from the three followers
Then it should transition to leader state, and the other candidate should remain in the candidate state.

## Considerations:

1. All data structures, log entries, current term, and vote counts should be protected against concurrent access.
2. Handling both RequestVoteRPCs and AppendEntriesRPCs concurrently while making it thread safe.
3. Making sure AppendEntriesRPCs are sent concurrently from the leader server to every other one.
4. Making sure the randomized timeouts are selected within a given range (pdf says between 150-300 ms)
5. Making sure hearbeat frequency is consistent.
6. Make sure the randomized election timeout is appropiate and greater than the heartbeat frequency.
7. Make sure transition periods from one role to another don't ever allow for 2 leaders at the same time.
8. If we're doing logs, making sure their indeces are consistent.
9. Making sure term updates occur when they're supposed to.
10. In case of tie in an election, making sure the new election cycle starts appropiately and not allowing for infinite split votes.

- Ensure each node has a unique identifier to prevent recording two votes from the same node.  
- When using the `RequestVoteRPC` to request a vote from another node, they will call the my `VoteResponseRPC` method.  
- If a vote request fails, it might retry and call the `VoteResponseRPC` method again. However, be cautious since a node can only vote once.  
- Heartbeats must occur more frequently (every 20ms) than the election timeout.  
- For heartbeats, iterate over every node in the neighbor list and call its `RequestAppendEntries` method with no arguments.  
- Keep track of the leader by storing the leader's unique ID.  
- In step 7, send an RPC every 100ms and ensure that no election starts after 300ms.  
- Ensure all RPC methods are asynchronous.  