using UnityEngine;
using System.Collections.Generic;

public class SeatManager : MonoBehaviour
{
    public static SeatManager Instance;

    public List<Seat> seats = new List<Seat>();

    void Awake()
    {
        Instance = this;
    }

    public Seat GetAvailableSeat()
    {
        foreach (Seat seat in seats)
        {
            if (seat.IsAvailable())
                return seat;
        }

        return null;
    }
}