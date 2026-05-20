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
        /// <typeparam name="T">The type to bind</typeparam>

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
        /// <param name="instance">The instance to bind</param>
        /// <param name="priority">The priority of the binding</param>
        /// <typeparam name="T">The type to bind the error message to.</typeparam>
        public static void BindInstance<T>(T instance, ModioServicePriority priority = ModioServicePriority.DeveloperOverride)
        {
            Bind<T>().FromInstance(instance, priority);
        }

        /// <summary>
        /// Binds an error message to a type, so that if that type is attempted to be resolved without a proper binding, the error message will be logged and included in the exception.
        /// </summary>
        /// <param name="message">The error message to log and include in the exception when resolution of the type fails due to missing bindings.</param>
        /// <param name="priority">The priority of the binding</param>
        /// <typeparam name="T">The type to bind the error message to.</typeparam>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <remarks> This is intended to make it easier for users to understand what they need to do to properly set up the Plugin when they encounter common issues with missing bindings.</remarks>
        public static void BindErrorMessage<T>(string message, ModioServicePriority priority = ModioServicePriority.Fallback)
        {
            Bind<T>().FromMethod(
                () =>
                {
                    ModioLog.Error?.Log(message);
                    throw new KeyNotFoundException($"Could not resolve type {typeof(T).FullName}. {message}");
                }, priority);
        }

        /// <summary>
        /// Removes all bindings of all types with the specified priority. If multiple bindings of the same priority exist, all of them will be removed.
        /// </summary>
        /// <param name="priority">The priority of the bindings to remove</param>
        internal static void RemoveAllBindingsWithPriority(ModioServicePriority priority)
        {
            foreach (Type type in new List<Type>(Bindings.Keys))
            {
                var bindings = Bindings[type];
                bindings.RemoveAllWithPriority(priority);
                if (bindings.BindingCount == 0) Bindings.Remove(type);
            }
        }

        /// <summary>
        /// Resolves an instance of type T using the current bindings.
        /// </summary>
        /// <typeparam name="T">The type to resolve</typeparam>
        /// <returns>>An instance of type T</returns>
        /// <remarks> If multiple bindings exist, the one with the highest priority will be used. If multiple bindings of the same priority exist, the last one added will be used.</remarks>
        public static T Resolve<T>()
        {
            IResolveType<T> dependencyBindings = GetBindings<T>();
            
            return dependencyBindings.Resolve();
        }

        /// <summary>
        /// Resolves an instance of type T using the current bindings, returning true if successful and false if no valid bindings were found.
        /// </summary>
        /// <param name="result">When the method returns, contains the resolved instance of type T if the resolution was successful, or the default value of T if no valid bindings were found.</param>
        /// <typeparam name="T">The type to resolve</typeparam>
        /// <returns>True if the resolution was successful, false if no valid bindings were found.</returns>
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

        /// <summary>
        /// Gets the bindings for type T.
        /// </summary>
        /// <param name="createIfMissing">If true, the bindings will be created if they don't exist yet. If false, a KeyNotFoundException will be thrown if the bindings don't exist.</param>
        /// <typeparam name="T">The type to get the bindings for</typeparam>
        /// <returns> The bindings for type T</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <remarks> This can be used to manually inspect or modify the bindings for a type. Consider using <see cref="AddBindingChangedListener{T}"/> instead if you just want to be notified when the binding changes without needing to manually manage the bindings yourself.</remarks>
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

        /// <summary>
        /// Adds a listener that will be invoked whenever a new binding of type T is added.
        /// </summary>
        /// <param name="onNewValue">The listener to invoke when a new binding is added. </param>
        /// <param name="fireImmediatelyIfValueBound">If true, the listener will be immediately invoked with the current value of the binding (if any) when added. If false, the listener will only be invoked on future changes to the binding.</param>
        /// <typeparam name="T">The type of the binding to listen for changes on</typeparam>
        public static void AddBindingChangedListener<T>(Action<T> onNewValue, bool fireImmediatelyIfValueBound = true)
        {
            IResolveType<T> resolveType = GetBindings<T>(true);
            resolveType.OnNewBinding += onNewValue;

            if (fireImmediatelyIfValueBound && resolveType.TryResolve(out T value)) onNewValue.Invoke(value);
        }
        
        /// <summary>
        /// Removes a listener added with <see cref="AddBindingChangedListener{T}"/> so that it will no longer be invoked when the binding changes.
        /// </summary>
        /// <param name="onNewValue">The listener to remove</param>
        /// <typeparam name="T">The type of the binding the listener was added to</typeparam>
        /// <remarks> Note that this will not prevent the listener from being invoked if the binding has already changed and the listener was not yet removed, so consider using <see cref="RemoveBindingWithPriority{T}"/> if you want to ensure a binding is no longer used at all.</remarks>
        public static void RemoveBindingChangedListener<T>(Action<T> onNewValue)
        {
            IResolveType<T> resolveType = GetBindings<T>(true);
            resolveType.OnNewBinding -= onNewValue;
        }

        /// <summary>
        /// Removes all bindings of type T, regardless of priority.
        /// </summary>
        /// <typeparam name="T">The type of the bindings to remove</typeparam>
        /// <remarks> Use with caution, as this may cause issues if done at the wrong time. Consider using <see cref="RemoveBindingWithPriority{T}"/> instead if you only want to remove specific bindings.</remarks>
        public static void RemoveAllBindingsOfType<T>()
        {
            if (!Bindings.TryGetValue(typeof(T), out ServiceBindings untypedDependencies))
                return;
            untypedDependencies.RemoveAllBindings();
        }
        
        /// <summary>
        /// Removes binding of type T with the specified priority. If multiple bindings of the same priority exist, all of them will be removed.
        /// </summary>
        /// <param name="priority">The priority of the bindings to remove</param>
        /// <typeparam name="T">The type of the bindings to remove</typeparam>
        internal static void RemoveBindingWithPriority<T>(ModioServicePriority priority)
        {
            if (!Bindings.TryGetValue(typeof(T), out ServiceBindings untypedDependencies))
                return;
            untypedDependencies.RemoveAllWithPriority(priority);
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

            IEnumerable<(T service, ModioServicePriority priority)> ResolveAll();
        }
        
        abstract class ServiceBindings
        {
            public abstract void RemoveAllWithPriority(ModioServicePriority priority);

            public abstract void RemoveAllBindings();
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

                try
                {
                    _value = _factory();
                }
                finally
                {
                    _runningFactoryMethod = false;
                }

                return _value;
            }
            
            public bool TryResolve(out T value)
            {
                if (_value != null || _factory == null)
                {
                    value = _value;
                    return true;
                }

                if (_runningFactoryMethod)
                {
                    ModioLog.Error?.Log($"Cyclic dependency detected when resolving type {typeof(T).FullName}. This will cause issues.");
                    value = default(T);
                    return false;
                }

                _runningFactoryMethod = true;

                try
                {
                    value = _value = _factory();
                }
                catch(KeyNotFoundException)
                {
                    value = default(T);
                    return false;
                }
                finally
                {
                    _runningFactoryMethod = false;
                }

                return true;
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

                InvokeNewBinding();
                return binding;
            }
            
            public Binding<T> FromMethod(Func<T> factory, ModioServicePriority priority, Func<bool> condition = null)
            {
                var binding = new Binding<T>(factory, priority, condition);
                Bindings.Add(binding);
                
                if(priority > ModioServicePriority.Fallback + 1)
                    InvokeNewBinding();
                
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
                else if (binding.Condition != null) {
                    Func<bool> prevCondition = condition;
                    condition = () => prevCondition() && binding.Condition();
                }
                
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

            public override void RemoveAllBindings() => Bindings.Clear();

            void InvokeNewBinding()
            {
                if (OnNewBinding == null) return;

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

                return topBinding.TryResolve(out value);
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
