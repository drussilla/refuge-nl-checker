namespace RefugeNlChecker;
#pragma warning disable CS8618
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

public class Request
{
    public Day day { get; set; }
    public Place place { get; set; }
    public AppointmentOptions appointment_options { get; set; }
    public Info info { get; set; }
    public Confirm confirm { get; set; }
}

public class LocationData
{
    public string unique_id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public string address { get; set; }
    public object link { get; set; }
}

public class Response
{
    public string unique_id { get; set; }
    public string time { get; set; }
    public string date { get; set; }
    public string location { get; set; }
    public LocationData location_data { get; set; }

    public override int GetHashCode()
    {
        return HashCode.Combine(time, date, location);
    }

    public override bool Equals(object? obj)
    {
        return GetHashCode() == obj?.GetHashCode();
    }
}
#pragma warning restore CS8618