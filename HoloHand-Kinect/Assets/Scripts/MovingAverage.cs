using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


class MovingAverage
{

    public int Period = 5;
    private Queue<float> buffer = new Queue<float>();

    public void Push(float quote)
    {
        if (buffer.Count == Period)
            buffer.Dequeue();
        buffer.Enqueue(quote);
    }

    public void Clear()
    {
        buffer.Clear();
    }

    public float Average {
        get {
            if (buffer.Count == 0) return 0;
            return buffer.Average();
        }
    }      
}
