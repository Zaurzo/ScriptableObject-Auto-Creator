using System;

namespace SOAutoCreator
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AutoCreateAssetAttribute : Attribute
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
    }
}