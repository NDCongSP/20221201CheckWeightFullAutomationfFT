using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking.Models.Entities
{
    public class ApplicationDbContextWL:DbContext
    {
        public ApplicationDbContextWL(string nameOrConnectionString) : base(nameOrConnectionString)
        {
        }

        protected ApplicationDbContextWL()
        {
        }
    }
}
