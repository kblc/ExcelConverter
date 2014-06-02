using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExelConverter.Core.DataObjects
{
    public class User
    {
        public long Id { get; set; }

        public string Login { get; set; }

        public string Name { get; set; }
        public string Surname { get; set; }
        public string Patronymic { get; set; }

        public string Password { get; set; }

        public string FullName
        { 
            get
            {
                return
                    string.Format("{0}{1}{2}"
                        , string.IsNullOrWhiteSpace(Surname) ? string.Empty : " " + Surname
                        , string.IsNullOrWhiteSpace(Name) ? string.Empty : " " + Name
                        , string.IsNullOrWhiteSpace(Patronymic) ? string.Empty : " " + Patronymic).Trim();
            } 
        }

        public override string ToString()
        {
            return Login + (string.IsNullOrWhiteSpace(FullName) ? string.Empty : " (" + FullName + ")");
        }

        //public Role Role { get; set; }
    }
}
