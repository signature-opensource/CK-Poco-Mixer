using CK.Core;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CK.Poco.Mixer
{
    /// <summary>
    /// Composite of multiple <see cref="BasePocoMixer"/>.
    /// <para>
    /// Subordinate mixers' are called in depth-first order.
    /// </para>
    /// <para>
    /// This class may be specialized to override <see cref="AcceptHookAsync(IActivityMonitor, BasePocoMixer.AcceptContext)"/>,
    /// <see cref="AfterAcceptAsync(IActivityMonitor, BasePocoMixer.AcceptContext)"/> and <see cref="ProcessAsync(IActivityMonitor, BasePocoMixer.ProcessContext)"/>.
    /// </para>
    /// </summary>
    public class CompositePocoMixer : BasePocoMixer<CompositePocoMixerConfiguration>
    {
        readonly ImmutableArray<BasePocoMixer> _children;

        internal CompositePocoMixer( CompositePocoMixerConfiguration configuration, ImmutableArray<BasePocoMixer> mixers )
            : base( configuration )
        {
            _children = mixers;
        }

        /// <summary>
        /// Calls <see cref="AcceptHookAsync"/> and if the input is accepted immediately returns.
        /// If AcceptHookAsync doesn't accept, then all the subordinated mixers are called until the input is accepted
        /// and <see cref="AfterAcceptAsync(IActivityMonitor, AcceptContext)"/> is called.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="context">The accept context.</param>
        /// <returns>The awaitable.</returns>
        public override async ValueTask AcceptAsync( IActivityMonitor monitor, AcceptContext context )
        {
            using var g = context.UserMessages?.OpenInfo( $"Input submitted to '{Configuration.Name}'." );
            await AcceptHookAsync( monitor, context ).ConfigureAwait( false );
            if( context.IsAccepted ) return;
            foreach( var c in _children )
            {
                await c.AcceptAsync( monitor, context ).ConfigureAwait( false );
                if( context.IsAccepted ) break;
            }
            await AfterAcceptAsync( monitor, context ).ConfigureAwait( false );
        }

        /// <summary>
        /// Extension point called before calling children's AcceptAsync. Does nothing by default. 
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="context">The accept context.</param>
        /// <returns>The awaitable.</returns>
        protected virtual ValueTask AcceptHookAsync( IActivityMonitor monitor, AcceptContext context ) => default;

        /// <summary>
        /// Always called after the children's AcceptAsync method calls: the <paramref name="context"/> may
        /// already be accepted.
        /// <para>
        /// This extension point can alter the accept context in any way, including accepting it: in such case
        /// <see cref="ProcessAsync(IActivityMonitor, ProcessContext)"/> must be overridden.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="context">The accept context.</param>
        /// <returns>The awaitable.</returns>
        protected virtual ValueTask AfterAcceptAsync( IActivityMonitor monitor, AcceptContext context ) => default;

        /// <summary>
        /// Called if and only if <see cref="AcceptHookAsync(IActivityMonitor, AcceptContext)"/> or <see cref="AfterAcceptAsync(IActivityMonitor, AcceptContext)"/>
        /// accepted the input. Does nothing by default.
        /// <para></para>
        /// This must be overridden when and only when AcceptHookAsync or AfterAcceptAsync are overridden.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="context">The context.</param>
        /// <returns>The awaitable.</returns>
        public override ValueTask ProcessAsync( IActivityMonitor monitor, ProcessContext context ) => default;
    }

}
