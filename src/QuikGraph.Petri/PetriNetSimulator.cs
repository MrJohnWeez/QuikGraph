using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace QuikGraph.Petri
{
    /// <summary>
    /// Petri Net simulator.
    /// </summary>
    /// <typeparam name="TToken">Token type.</typeparam>
#if SUPPORTS_SERIALIZATION
    [Serializable]
#endif
    public sealed class PetriNetSimulator<TToken>
    {
        [NotNull]
        private Dictionary<ITransition<TToken>, TransitionBuffer> _transitionBuffers = new Dictionary<ITransition<TToken>, TransitionBuffer>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PetriNetSimulator{TToken}"/> class.
        /// </summary>
        /// <param name="net">Petri net to simulate.</param>
        public PetriNetSimulator([NotNull] IPetriNet<TToken> net)
        {
            Net = net ?? throw new ArgumentNullException(nameof(net));
        }

        /// <summary>
        /// Petri Net.
        /// </summary>
        [NotNull]
        public IPetriNet<TToken> Net { get; }

        /// <summary>
        /// Initializes simulator.
        /// </summary>
        public void Initialize()
        {
            _transitionBuffers.Clear();
            foreach (ITransition<TToken> transition in Net.Transitions)
            {
                _transitionBuffers.Add(transition, new TransitionBuffer());
            }
        }

        /// <summary>
        /// Simulates a step.
        /// </summary>
        public void SimulateStep()
        {
            // First step, iterate over arcs and gather tokens in transitions
            foreach (IArc<TToken> arc in Net.Arcs)
            {
                if (!arc.IsInputArc)
                    continue;

                IList<TToken> tokens = _transitionBuffers[arc.Transition].Tokens;
                // Get annotated tokens
                IList<TToken> annotatedTokens = arc.Annotation.Evaluate(arc.Place.Marking);
                // Add annotated tokens
                foreach (TToken annotatedToken in annotatedTokens)
                {
                    tokens.Add(annotatedToken);
                }
            }

            // Second step, see which transition was enabled
            foreach (ITransition<TToken> transition in Net.Transitions)
            {
                // Get buffered tokens
                IList<TToken> tokens = _transitionBuffers[transition].Tokens;
                // Check if enabled, store value
                _transitionBuffers[transition].Enabled = transition.Condition.IsEnabled(tokens);
            }

            // Third step, iterate over the arcs
            foreach (IArc<TToken> arc in Net.Arcs)
            {
                if (!_transitionBuffers[arc.Transition].Enabled)
                    continue;

                if (arc.IsInputArc)
                {
                    // Get annotated tokens
                    IList<TToken> annotatedTokens = arc.Annotation.Evaluate(arc.Place.Marking);
                    // Remove annotated comments from source place
                    foreach (TToken annotatedToken in annotatedTokens)
                    {
                        arc.Place.Marking.Remove(annotatedToken);
                    }
                }
                else
                {
                    IList<TToken> tokens = _transitionBuffers[arc.Transition].Tokens;
                    // Get annotated tokens
                    IList<TToken> annotatedTokens = arc.Annotation.Evaluate(tokens);
                    // IList<Token> annotated comments to target place
                    foreach (TToken annotatedToken in annotatedTokens)
                    {
                        arc.Place.Marking.Add(annotatedToken);
                    }
                }
            }

            // Step four, clear buffers
            foreach (ITransition<TToken> transition in Net.Transitions)
            {
                _transitionBuffers[transition].Tokens.Clear();
                _transitionBuffers[transition].Enabled = false;
            }
        }

        private sealed class TransitionBuffer
        {
            [NotNull, ItemNotNull]
            public IList<TToken> Tokens { get; } = new List<TToken>();

            public bool Enabled { get; set; } = true;
        }
    }
}