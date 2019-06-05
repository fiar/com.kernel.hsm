using System;
using System.Collections.Generic;
using System.Reflection;

namespace Kernel.HSM
{
    public static class StateBuilderExtensions
    {
        private static List<object> _awakes = new List<object>();
        private static List<object> _starts = new List<object>();

        public static class ConcreteStateDeclarations
        {
            public class ConcreteStateInfo
            {
                public MethodInfo Awake { get; set; }
                public MethodInfo Start { get; set; }
                public MethodInfo OnDestroy { get; set; }
                public MethodInfo OnEnter { get; set; }
                public MethodInfo OnExit { get; set; }
                public MethodInfo Update { get; set; }
            }

            public static ConcreteStateInfo ResolveStateInfo(Type type)
            {
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                return new ConcreteStateInfo
                {
                    Awake = type.GetMethod("Awake", flags),
                    Start = type.GetMethod("Start", flags),
                    OnDestroy = type.GetMethod("OnDestroy", flags),
                    OnEnter = type.GetMethod("OnEnter", flags),
                    OnExit = type.GetMethod("OnExit", flags),
                    Update = type.GetMethod("Update", flags)
                };
            }
        }

        public static StateBuilder<TParent> Concrete<TParent>(this StateBuilder<TParent> builder, object concreteState)
        {
            if (concreteState == null) return builder;

            var info = ConcreteStateDeclarations.ResolveStateInfo(concreteState.GetType());
            bool isEntered = false;

            return builder
                ///
                /// Awake State
                .Awake(state =>
                {
                    if (!_awakes.Contains(concreteState))
                    {
                        _awakes.Add(concreteState);
                        if (info.Awake != null && concreteState != null)
                        {
                            info.Awake.Invoke(concreteState, null);
                        }
                    }
                })
                ///
                /// Start State
                .Start(state =>
                {
                    if (!_starts.Contains(concreteState))
                    {
                        _starts.Add(concreteState);
                        if (info.Start != null && concreteState != null)
                        {
                            info.Start.Invoke(concreteState, null);
                        }
                    }
                })
                ///
                /// Destroy State
                .Destroy(state =>
                {
                    if (_awakes.Contains(concreteState))
                    {
                        _awakes.Remove(concreteState);
                        if (info.OnDestroy != null && concreteState != null)
                        {
                            info.OnDestroy.Invoke(concreteState, null);
                        }
                    }
                })
                ///
                /// Enter State
                .Enter(state =>
                {
                    isEntered = true;
                    if (info.OnEnter != null && concreteState != null)
                    {
                        info.OnEnter.Invoke(concreteState, null);
                    }
                })
                ///
                /// Exit State
                .Exit(state =>
                {
                    if (isEntered)
                    {
                        isEntered = false;
                        if (info.OnExit != null && concreteState != null)
                        {
                            info.OnExit.Invoke(concreteState, null);
                        }
                    }
                })
                ///
                /// Update State
                .Update(state =>
                {
                    if (info.Update != null && concreteState != null)
                    {
                        info.Update.Invoke(concreteState, null);
                    }
                });
        }
    }
}
