public interface INode
{
    int Term { get; set; }
    public bool RequestToVoteFor(INode n);
}