using System;

namespace VillageAI
{
    public enum Status { Failure, Success, Running }
    public interface INode { Status Tick(float delta); void Reset(); }
    public sealed class ActionNode : INode
    {
        private readonly Func<float, Status> action; private readonly Action reset;
        public ActionNode(Func<float, Status> action, Action reset = null) { this.action = action; this.reset = reset; }
        public Status Tick(float delta) => action(delta);
        public void Reset() { reset?.Invoke(); }
    }
    public sealed class Sequence : INode
    {
        private readonly INode[] nodes; private int index;
        public Sequence(params INode[] nodes) { this.nodes = nodes; }
        public Status Tick(float delta)
        {
            while (index < nodes.Length)
            {
                var result = nodes[index].Tick(delta);
                if (result == Status.Running) return result;
                if (result == Status.Failure) { Reset(); return result; }
                index++;
            }
            Reset(); return Status.Success;
        }
        public void Reset() { foreach (var node in nodes) node.Reset(); index = 0; }
    }
    public sealed class Guard : INode
    {
        private readonly Func<bool> condition; private readonly INode child; private readonly Action leave;
        public Guard(Func<bool> condition, INode child, Action leave = null) { this.condition = condition; this.child = child; this.leave = leave; }
        public Status Tick(float delta)
        { if (!condition()) { Reset(); return Status.Failure; } var result = child.Tick(delta); if (result != Status.Running) leave?.Invoke(); return result; }
        public void Reset() { child.Reset(); leave?.Invoke(); }
    }
    public sealed class ReactiveSelector : INode
    {
        private readonly INode[] children; private int active = -1;
        public ReactiveSelector(params INode[] children) { this.children = children; }
        public Status Tick(float delta)
        {
            for (int i = 0; i < children.Length; i++)
            {
                var result = children[i].Tick(delta);
                if (result == Status.Failure) continue;
                if (active >= 0 && active != i) children[active].Reset();
                active = result == Status.Running ? i : -1; return result;
            }
            Reset(); return Status.Failure;
        }
        public void Reset() { foreach (var node in children) node.Reset(); active = -1; }
    }
}
