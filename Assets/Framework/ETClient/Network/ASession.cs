using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public abstract class ASession : IDisposable
    {
        public long Id { get; set; }

        public virtual void Dispose() { }
    }
}
