using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Modio
{
    public static class ModioServices
    {
        static readonly Dictionary<Type, ServiceBindings> Bindings = new Dictionary<Type, ServiceBindings>();

#region PublicMethods
        
        /// <summary>
        /// Bind a service so that it can be accessed by other systems
        /// </summary>
        /// <example><code>
        /// ModioServices.Bind&lt;IWebBrowserHandler&gt;()
        ///              .FromNew&lt;MyCustomWebBrowserHandler&gt;(ModioServicePriority.DeveloperOverride);
        /// </code></example>
        [Pure]
        public static IBindType<T> Bind<T>()
        {
            if (!Bindings.TryGetValue(typeof(T), out ServiceBindings bindings)) 
                Bindings[typeof(T)] = bindings = new ServiceBindings<T>();

            return (IBindType<T>)bindings;
        }

        /// <summary>
        /// Convenience wrapper to bind an instance. The same as writing
        /// <code>Bind&lt;T&gt;().FromInstance(instance, priority);</code>
        /// </summary>
        public static void BindInstance<T>(T instance, ModioServicePriority priority = ModioServicePriority.DeveloperOverride)
        {
            Bind<T>().FromInstance(instance, priority);
        }

        public static void BindErrorMessage<T>(string message, ModioServicePriority priority = ModioServicePriority.Fallback)
        {
            Bind<T>().FromMethod(
                () =>
                {
                    ModioLog.Error?.Log(message);
                    throw new KeyNotFoundException($"Could not resolve type {typeof(T).FullName}. {message}");
                }, priority);
        }

        internal static void RemoveAllBindingsWithPriority(ModioServicePriority priority)
        {
            foreach (Type type in new List<Type>(Bindings.Keys))
            {
                var bindings = Bindings[type];
                bindings.RemoveAllWithPriority(priority);
                if (bindings.BindingCount == 0) Bindings.Remove(type);
            }
        }

        public static T Resolve<T>()
        {
            IResolveType<T> dependencyBindings = GetBindings<T>();
            
            return dependencyBindings.Resolve();
        }

        public static bool TryResolve<T>(out T result)
        {
            if (!Bindings.TryGetValue(typeof(T), out ServiceBindings untypedDependencies))
            {
                result = default(T);
                return false;
            }

            var dependencyBindings = (ServiceBindings<T>)untypedDependencies;
            return dependencyBindings.TryResolve(out result);
        }

        public static IResolveType<T> GetBindings<T>(bool createIfMissing = false)
        {
            if (!Bindings.TryGetValue(typeof(T), out ServiceBindings untypedDependencies))
            {
                if(createIfMissing)
                    Bindings[typeof(T)] = untypedDependencies = new ServiceBindings<T>();
                else
                    throw new KeyNotFoundException($"Could not resolve type {typeof(T).FullName}");
            }

            var dependencyBindings = (ServiceBindings<T>)untypedDependencies;
            return dependencyBindings;
        }

        public static void AddBindingChangedListener<T>(Action<T> onNewValue, bool fireImmediatelyIfValueBound = true)
        {
            IResolveType<T> resolveType = GetBindings<T>(true);
            resolveType.OnNewBinding += onNewValue;

            if (fireImmediatelyIfValueBound && resolveType.TryResolve(out T value)) onNewValue.Invoke(value);
        }
        
        public static void RemoveBindingChangedListener<T>(Action<T> onNewValue)
        {
            IResolveType<T> resolveType = GetBindings<T>(true);
            resolveType.OnNewBinding -= onNewValue;
        }

#endregion

#region PublicInterfaces

        public interface IBindType<T>
        {
            Binding<T> FromInstance(T value, ModioServicePriority priority = ModioServicePriority.DeveloperOverride, Func<bool> condition = null);

            Binding<T> FromMethod(Func<T> factory, ModioServicePriority priority = ModioServicePriority.DeveloperOverride, Func<bool> condition = null);
            
            Binding<T> FromNew<TResolved>(ModioServicePriority priority = ModioServicePriority.DeveloperOverride, Func<bool> condition = null) where TResolved : T, new();

            Binding<T> FromNew(Type type, ModioServicePriority priority = ModioServicePriority.DeveloperOverride, Func<bool> condition = null);

            // intended to enable WithInterfaces
            /*internal*/ Binding<T> WithOtherBinding<TOther>(Binding<TOther> binding, Func<bool> condition = null);

            IBindType<T> WithInterfaces<TI1>(Func<bool> condition = null);
            IBindType<T> WithInterfaces<TI1, TI2>(Func<bool> condition = null);
            IBindType<T> WithInterfaces<TI1, TI2, TI3>(Func<bool> condition = null);
        }
        
        public interface IResolveType<T>
        {
            T Resolve();

            bool TryResolve(out T value);

            event Action<T> OnNewBinding;

            IEnumerable<(T, ModioServicePriority)> ResolveAll();
        }
        
        abstract class ServiceBindings
        {
            public abstract void RemoveAllWithPriority(ModioServicePriority priority);
            public abstract int BindingCount { get; }
        }

        public class Binding<T>
        {
            public readonly ModioServicePriority Priority;
            public readonly Func<bool> Condition;

            readonly Func<T> _factory;
            
            T _value;
            bool _runningFactoryMethod;

            public Binding(T value, ModioServicePriority priority, Func<bool> condition = null)
            {
                _value = value;
                Priority = priority;
                Condition = condition;
            }

            public Binding(Func<T> factory, ModioServicePriority priority, Func<bool> condition = null)
            {
                _factory = factory;
                Priority = priority;
                Condition = condition;
            }
            
            public T Resolve()
            {
                if (_value != null || _factory == null) return _value;

                if (_runningFactoryMethod)
                {
                    ModioLog.Error?.Log($"Cyclic dependency detected when resolving type {typeof(T).FullName}. This will cause issues.");
                    return default(T);
                }
                
                _runningFactoryMethod = true;
                _value = _factory();
                _runningFactoryMethod = false;

                return _value;
            }
        }
        
        class ServiceBindings<T> : ServiceBindings, IBindType<T>, IResolveType<T>
        {
            public readonly List<Binding<T>> Bindings = new List<Binding<T>>();
            public override int BindingCount => Bindings.Count;
            public event Action<T> OnNewBinding;

            public Binding<T> FromInstance(T value, ModioServicePriority priority = ModioServicePriority.DeveloperOverride, Func<bool> condition = null)
            {
                var binding = new Binding<T>(value, priority, condition);
                Bindings.Add(binding);

                InvokeNewBindingIfHighestPriority(priority);
                return binding;
            }
            
            public Binding<T> FromMethod(Func<T> factory, ModioServicePriority priority, Func<bool> condition = null)
            {
                var binding = new Binding<T>(factory, priority, condition);
                Bindings.Add(binding);
                
                InvokeNewBindingIfHighestPriority(priority);
                return binding;
            }

            public Binding<T> FromNew<TResolved>(ModioServicePriority priority, Func<bool> condition = null) where TResolved : T, new()
            {
                return FromMethod(() => new TResolved(), priority, condition);
            }

            public Binding<T> FromNew(Type type, ModioServicePriority priority, Func<bool> condition = null)
            {
                if(!typeof(T).IsAssignableFrom(type))
                    throw new ArgumentException($"Type '{type.FullName}' is not assignable to '{typeof(T).FullName}'");
                return FromMethod(() => (T)Activator.CreateInstance(type), priority, condition);
            }

            public Binding<T> WithOtherBinding<TOther>(Binding<TOther> binding, Func<bool> condition = null)
            {
                if (!typeof(T).IsAssignableFrom(typeof(TOther)))
                {
                    throw new ArgumentException("Type '" + typeof(T).FullName + "' is not assignable to '" + typeof(TOther).FullName + "'");
                }
                if(condition == null) condition = binding.Condition;
                else if (binding.Condition != null)
                    // ReSharper disable once AccessToModifiedClosure (that's the point)
                    condition = () => condition() && binding.Condition();
                
                return FromMethod(() => (T)(object)binding.Resolve(), binding.Priority, condition);
            }

            public IBindType<T> WithInterfaces<TI1>(Func<bool> condition = null)
            {
                return new MultiBind(this, b =>
                {
                    Bind<TI1>().WithOtherBinding(b, condition);
                });
            }
            public IBindType<T> WithInterfaces<TI1, TI2>(Func<bool> condition = null)
            {
                return new MultiBind(this, b =>
                {
                    Bind<TI1>().WithOtherBinding(b, condition);
                    Bind<TI2>().WithOtherBinding(b, condition);
                });
            }
            public IBindType<T> WithInterfaces<TI1, TI2, TI3>(Func<bool> condition = null)
            {
                return new MultiBind(this, b =>
                {
                    Bind<TI1>().WithOtherBinding(b, condition);
                    Bind<TI2>().WithOtherBinding(b, condition);
                    Bind<TI3>().WithOtherBinding(b, condition);
                });
            }

            class MultiBind : IBindType<T> {
                readonly ServiceBindings<T> _coreBinding;
                readonly Action<Binding<T>> _afterBinding;

                public MultiBind(ServiceBindings<T> coreBinding, Action<Binding<T>> afterBinding)
                {
                    _coreBinding = coreBinding;
                    _afterBinding = afterBinding;
                }

                public Binding<T> FromInstance(T value, ModioServicePriority priority = ModioServicePriority.DeveloperOverride, Func<bool> condition = null) => BindWith(_coreBinding.FromInstance(value, priority, condition));

                public Binding<T> FromMethod(Func<T> factory, ModioServicePriority priority = ModioServicePriority.DeveloperOverride, Func<bool> condition = null) => BindWith(_coreBinding.FromMethod(factory, priority, condition));

                public Binding<T> FromNew<TResolved>(ModioServicePriority priority = ModioServicePriority.DeveloperOverride, Func<bool> condition = null) where TResolved : T, new() => BindWith(_coreBinding.FromNew<TResolved>(priority, condition));

                public Binding<T> FromNew(Type type, ModioServicePriority priority = ModioServicePriority.DeveloperOverride, Func<bool> condition = null) => BindWith(_coreBinding.FromNew(type, priority, condition));

                public Binding<T> WithOtherBinding<TOther>(Binding<TOther> binding, Func<bool> condition = null) => BindWith(_coreBinding.WithOtherBinding(binding, condition));

                public IBindType<T> WithInterfaces<TI1>(Func<bool> condition = null)
                {
                    return new MultiBind(_coreBinding, b =>
                    {
                        _afterBinding(b);
                        Bind<TI1>().WithOtherBinding(b, condition);
                    });
                }

                public IBindType<T> WithInterfaces<TI1, TI2>(Func<bool> condition = null)
                {
                    return new MultiBind(_coreBinding, b =>
                    {
                        _afterBinding(b);
                        Bind<TI1>().WithOtherBinding(b, condition);
                        Bind<TI2>().WithOtherBinding(b, condition);
                    });
                }

                public IBindType<T> WithInterfaces<TI1, TI2, TI3>(Func<bool> condition = null)
                {
                    return new MultiBind(_coreBinding, b =>
                    {
                        _afterBinding(b);
                        Bind<TI1>().WithOtherBinding(b, condition);
                        Bind<TI2>().WithOtherBinding(b, condition);
                        Bind<TI3>().WithOtherBinding(b, condition);
                    });
                }

                Binding<T> BindWith(Binding<T> core)
                {
                    _afterBinding.Invoke(core);
                    return core;
                }
            }

            public override void RemoveAllWithPriority(ModioServicePriority priority)
            {
                for (var i = Bindings.Count - 1; i >= 0; i--)
                {
                    if(Bindings[i].Priority == priority)
                        Bindings.RemoveAt(i);
                }
            }

            void InvokeNewBindingIfHighestPriority(ModioServicePriority priority)
            {
                if (OnNewBinding == null) return;

                foreach (Binding<T> binding in Bindings)
                    if(binding.Priority > priority)
                        return;

                if(TryResolve(out T value))
                    OnNewBinding(value);
            }

            public T Resolve() => TryResolve(out T value) ? value : throw new KeyNotFoundException($"Could not resolve type {typeof(T).FullName}");

            public bool TryResolve(out T value)
            {
                ModioServicePriority? topPriority = null;
                Binding<T> topBinding = null;

                foreach (Binding<T> binding in Bindings)
                {
                    //Note that we take the last match with equal priority
                    if (topPriority != null && topPriority.Value > binding.Priority) continue;
                    if(binding.Condition != null && !binding.Condition()) continue;
                    topPriority = binding.Priority;
                    topBinding = binding;
                }
            
                if (topPriority == null)
                {
                    value = default(T);
                    return false;
                }

                value = topBinding.Resolve();
                return true;
            }
            
            public IEnumerable<(T, ModioServicePriority)> ResolveAll()
            {
                return Bindings
                       .Where(b => b.Condition == null || b.Condition())
                       .Select(b => (b.Resolve(), b.Priority));
            }
        }
#endregion

    }
}
