using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking.Models.Entities
{
    public class ApplicationDbContextDogeWH : DbContext
    {
        public ApplicationDbContextDogeWH(string nameOrConnectionString) : base(nameOrConnectionString)
        {
        }

        protected ApplicationDbContextDogeWH()
        {
        }
    }
}
