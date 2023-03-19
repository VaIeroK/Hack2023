using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    public class Person
    {
        public Person() { }
        public Person(string name)
        {
            this.name = name;
        }
        public string name;
        public string id;
        public double plusRep = 0;
        public double minusRep = 0;
        public double totalRep = 0;


        public double activeInchat = 0;
        public double typeCommunecation = 0;

        public int overwork = 0;
    }
}
