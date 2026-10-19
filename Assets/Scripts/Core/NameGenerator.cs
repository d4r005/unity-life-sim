using System;
using System.Collections.Generic;

/// <summary>Nombres en español para familiares generados.</summary>
public static class NameGenerator
{
    private static readonly string[] maleNames =
        { "Juan", "Carlos", "Miguel", "Andrés", "Diego", "Javier", "Luis", "Pablo", "Sergio", "Mateo",
          "Alejandro", "Ramón", "Fernando", "Emiliano", "Ricardo", "Tomás", "Ángel", "Rodrigo", "Bruno", "Héctor" };

    private static readonly string[] femaleNames =
        { "María", "Lucía", "Carmen", "Sofía", "Elena", "Paula", "Ana", "Diana", "Rosa", "Valeria",
          "Gabriela", "Ximena", "Regina", "Renata", "Fernanda", "Adriana", "Isabela", "Camila", "Silvia", "Esther" };

    private static readonly string[] surnames =
        { "García", "Hernández", "López", "Martínez", "Rodríguez", "Pérez", "Sánchez", "Ramírez",
          "Torres", "Flores", "Rivera", "Gómez", "Díaz", "Reyes", "Cruz", "Morales", "Ortiz", "Gutiérrez" };

    private static readonly string[] countries =
        { "Mexico", "Argentina", "Colombia", "España", "Chile", "Peru", "Estados Unidos", "Brasil" };

    public static string RandomMale(Random rng) { return maleNames[rng.Next(maleNames.Length)]; }
    public static string RandomFemale(Random rng) { return femaleNames[rng.Next(femaleNames.Length)]; }
    public static string RandomSurname(Random rng) { return surnames[rng.Next(surnames.Length)]; }
    public static string RandomCountry(Random rng) { return countries[rng.Next(countries.Length)]; }

    public static string RandomName(Random rng, string gender)
    {
        return gender == "F" ? RandomFemale(rng) : RandomMale(rng);
    }
}
