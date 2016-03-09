using System.Collections.Generic;
using System;
using Composite.Core.Types;
using System.Linq;


namespace Composite.C1Console.Security
{
    /// <summary>    
    /// </summary>
    /// <exclude />
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] 
    public abstract class ActionToken
    {
        /// <exclude />
        public abstract IEnumerable<PermissionType> PermissionTypes { get; }

        /// <exclude />
        public virtual string Serialize() { return ""; }

        /// <exclude />
        public virtual bool IgnoreEntityTokenLocking { get { return false; } }
    }



    internal static class ActionTokenExtensionMethods
    {
        private static Dictionary<Type, bool> _ignoreEntityTokenLockingCache = new Dictionary<Type, bool>();
        private static readonly object _lock = new object();

        public static bool IsIgnoreEntityTokenLocking(this ActionToken actionToken)
        {
            if (actionToken == null) throw new ArgumentNullException(nameof(actionToken));
            
            lock (_lock)
            {
                return actionToken.IgnoreEntityTokenLocking;
            }
        }
    }
}
