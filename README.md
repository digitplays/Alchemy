# Alchemy
.net core to mssql orm



 Person Hansel = new Person()
{ 
    Id=Guid.NewGuid(),
    Name = "Hansel",
    hasDinner = false
};
await A.Insert(Hansel, "Person");
Hansel = await A.Select<Person>(Hansel, "Person");
Hansel.hasDinner = true;
await A.Update(new Person(){Id=Hansel.Id} , Hansel, "Person");
await A.Delete(Hansel, "Person");
  
  List<Person> People = new List<Person>(){new Person(){ hasDinner = true }});
  People = await A.SelectMany(People, "Person");
