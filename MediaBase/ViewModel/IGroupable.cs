using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBase.ViewModel
{
    public interface IGroupable
    {
        /// <summary>
        /// Gets or sets a value where each bit represents a group.
        /// </summary>
        /// <remarks>
        /// The meaning of "group" is arbitrary and has no effect on the
        /// functionality of this object.
        /// </remarks>
        int GroupFlags { get; set; }

        bool CheckGroupFlag(int group);

        void ToggleGroupFlag(int group);
    }
}