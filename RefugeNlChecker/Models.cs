namespace RefugeNlChecker;

public class AppointmentOptions
{
    public string appointment { get; set; }
}

public class Confirm
{
}

public class Day
{
    public string date { get; set; }
    public string amount { get; set; }
}

public class Info
{
    public string telephone_number { get; set; }
}

public class Place
{
    public string postcode { get; set; }
}

public class Root
{
    public Day day { get; set; }
    public Place place { get; set; }
    public AppointmentOptions appointment_options { get; set; }
    public Info info { get; set; }
    public Confirm confirm { get; set; }
}