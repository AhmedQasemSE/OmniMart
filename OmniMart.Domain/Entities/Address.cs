using OmniMart.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniMart.Domain.Common;

public record Address
{
    public string City { get; private set; }
    public string Street { get; private set; }
    public string ZipCode { get; private set; }
    public string PhoneNumber { get; private set; }

    public Address(string city, string street, string zipCode, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("City is required.");
        if (string.IsNullOrWhiteSpace(street)) throw new ArgumentException("Street is required.");

        City = city;
        Street = street;
        ZipCode = zipCode;
        PhoneNumber = phoneNumber;
    }
#pragma warning disable CS8618
    protected Address() { }
#pragma warning disable CS8618

}