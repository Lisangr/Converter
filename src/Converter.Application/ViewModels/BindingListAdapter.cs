using System.Collections.Generic;
using System.ComponentModel;

namespace Converter.Application.ViewModels
{
    public class BindingListAdapter<T> : BindingList<T> where T : INotifyPropertyChanged
    {
        public BindingListAdapter()
        {
        }

        public BindingListAdapter(IList<T> list) : base(list)
        {
        }
    }
}
