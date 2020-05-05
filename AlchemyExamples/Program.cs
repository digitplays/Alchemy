using System;
using System.Collections.Generic;
using Tome;
using System.Threading;
using System.Threading.Tasks;
namespace AlchemyExamples
{
    class Program
    {
        Alchemy A = new Alchemy();
        static void Main(string[] args)
        {
            Program p = new Program();
            
            p.startProgram().Wait();

        }
        public async Task startProgram()
        {
            //Always set connection string
            A.connectionString = "Server={ServerName};Database={DatabaseName};AuthenticationMethod";
            //Set whether or not the application can modify database structure(add tables/columns, change column data types)
            A.AutoManageDatabaseStructure = true;
            //where logs are written
            A.sqlLog = @"\Logs";

            if(!await A.TestConnection())
            {
                return;
            }

            //Syntax examples(to see parameterization process look in the process classes notes)
            //lets create a person
            Person Hansel = new Person()
            { 
                Name = "Hansel"
            };
            Hansel = await A.Select<Person>(Hansel, "Person");
            if(Hansel == null) {
                Hansel = new Person()
                {
                    Id = Guid.NewGuid(),

                    BirthDay = DateTime.Today.AddDays(-9999),
                    hasEaten = true,
                    isEaten = false,
                    Name = "Hansel"
                };


            }




            Person Gretel = new Person()
            {
                Id = Guid.NewGuid(),

                BirthDay = DateTime.Today.AddDays(-9999),
                hasEaten = true,
                isEaten = false,
                Name = "Gretel"
            };


            //Now lets save Hansel and Gretel to a database
            await A.Insert(Gretel, "Person");
            await A.Insert(Hansel, "Person");


            //Hansel and Gretel find the witches house. we all know how this story goes


            Person Witch = new Person()
            {
                Id = Guid.NewGuid(),

                BirthDay = DateTime.Today.AddDays(-9999),
                hasEaten = false,
                isEaten = false,
                Name = "Witch",
                publicItems = new Trunk()
                {
                    Id = Guid.NewGuid(),
                    TrunkContents = new List<object>() { "Candy", "Treats" }

                }
            };


            await A.Insert(Witch, "Person");


            //the witch gives candy to the kids to fatten them up and once theyve eaten

            Hansel.publicItems = Witch.publicItems;
            Hansel.hasEaten = true;

            Gretel.publicItems = Witch.publicItems;
            Gretel.hasEaten = true;
            await A.Insert(Gretel, "Memories");
            Witch.publicItems = new Trunk() { Id = Guid.NewGuid(), TrunkContents = new List<object>() };

            await A.Update(new Person() { Id = Hansel.Id }, Hansel, "Person");
            await A.Update(new Person() { Id = Gretel.Id }, Gretel, "Person");
            await A.Update(new Person() { Id = Witch.Id }, Witch, "Person");



            //this is the part where the witch eats the kids
            Hansel.isEaten = true;
            Gretel.isEaten = true;

            await A.Update(new Person() { Id = Hansel.Id }, Hansel, "Person");
            await A.Update(new Person() { Id = Gretel.Id }, Gretel, "Person");
            //Hansel will probably remember this
            await A.Insert(Gretel, "Memories");


            Person G = Gretel;
            //She only captures and eats Gretel 
            Witch.publicItems.TrunkContents.Add(Gretel) ;
            await A.Update(new Person() { Id = Witch.Id }, Witch, "Person");


            //querying by ID isnt necessary, but if you think you make changes to the data, its the best practice so far
            await A.Delete(new Person() { Id = Gretel.Id }, "Person");

            await A.Insert(Gretel, "Memories");




            //even though Gretel was captured by the witch and most likely eaten she lives on in HanselMemory
            List<Person> HanselsMemoriesofGretel = await A.SelectMany(new List<Person> { new Person() { Id = Gretel.Id } }, "Memories");
            foreach(Person P__ in HanselsMemoriesofGretel)
            {
                //idk how memories work
            }





        }
    }


    public class Person
    {
        //NUllable data types are your parameters, as well as any customer objects(the entire person class)
        public Guid? Id { get; set; }
        public string Name { get; set; }

        public DateTime? BirthDay { get; set; }


        //since isMarried isnt nullable it wont be stored/queried against. use this to setup logic flow
        public bool isEaten { get; set; }
        //haSpouse is nullable, and therfore when a value is set on hasSpouse it is queried against.
        public bool? hasEaten { get; set; }


        //custom objects work the same asa the nullable objects, they simply get serialized to the DB
        public Trunk publicItems { get; set; }

        //private objects dont get stored, or queried. but can be useful for moving information around.
        private Trunk privateItems { get; set; }
    }

 

    public class Trunk
    {
        public Guid? Id { get; set; }

       public List<object> TrunkContents { get; set; }
    }

}
