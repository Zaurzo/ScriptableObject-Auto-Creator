using System;

namespace ZaurzoUtil
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class EnsureAssetExistsAttribute : Attribute
    {
        /// <summary>
        /// The folder of which this asset will be created in.
        /// </summary>
        public string initialFolder;

        /// <summary>
        /// The name of which this asset will be created with.
        /// Defaults to the name of the type.
        /// </summary>
        public string initialName;

        /// <summary>
        /// Determines if the asset can be moved to a different directory.
        /// </summary>
        public bool movable = true;
    }
}