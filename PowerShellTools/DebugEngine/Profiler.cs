using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using System.Threading.Tasks;

namespace PowerShellTools.DebugEngine
{
    public class Profiler
    {
        private Segment _root;
        private Segment _lastSegment;

        public string Instrument(string input)
        {
            Token[] tokens;
            ParseError[] errors;
            var ast = Parser.ParseInput(input, out tokens, out errors);

            foreach (var statement in ast.EndBlock.Statements)
            {
                
            }

            return null;
        }

        public Segment Begin(int line, int scope)
        {
            if (_root == null)
            {
                _root = new Segment(line, scope);
                _lastSegment = _root;
                return _lastSegment;
            }

            Segment segment = null;
            if (_lastSegment.Scope == scope)
            {
                segment = new Segment(line, scope, _lastSegment.Parent);
                segment.StartTiming();
                _lastSegment.Parent.Children.Add(segment);
            }

            if (_lastSegment.Scope > scope)
            {
                var parent = _lastSegment.Parent;
                for (var i = 0; i < (_lastSegment.Scope - scope - 1); i++)
                {
                    parent = parent.Parent;
                }

                segment = new Segment(line, scope, parent);
                segment.StartTiming();
                parent.Children.Add(segment);
            }

            if (_lastSegment.Scope < scope)
            {
                segment = new Segment(line, scope, _lastSegment);
                segment.StartTiming();
                _lastSegment.Children.Add(segment);
            }

            _lastSegment = segment;
            return segment;
        }

        public void End(Segment segment)
        {
            segment.EndTiming();
        }
    }

    public class Segment
    {
        public Segment(int line, int scope)
        {
            Line = line;
            Scope = scope;
            Children = new List<Segment>();
        }

        public Segment(int scope, int line, Segment parent)
        {
            Scope = scope;
            Line = line;
            Parent = parent;
            Children = new List<Segment>();
        }

        public void StartTiming()
        {
            _watch = new Stopwatch();
            _watch.Start();
        }

        public void EndTiming()
        {
            _watch.Stop();
            Timing = _watch.Elapsed;
        }

        private Stopwatch _watch;

        public int Line;
        public int Scope;
        public List<Segment> Children;
        public Segment Parent;
        public TimeSpan Timing;
    }
}
