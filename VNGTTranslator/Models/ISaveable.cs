using System.Windows.Controls;

namespace VNGTTranslator.Models
{
    /// <summary>
    /// indicate that the object can be saved , used for <see cref="Page" /> content
    /// because page unload event will not be triggered when windows is close
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// save the content of the object
        /// </summary>
        void Save();
    }
}