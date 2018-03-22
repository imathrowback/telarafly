using System.Collections;
using System.Collections.Generic;
using System.Threading;
public class ThreadedJob
{
    private bool m_IsDone = false;
    private object m_Handle = new object();
    private System.Threading.Thread m_Thread = null;

    public bool IsStarted
    {
        get
        {
            return m_Thread != null;
        }
    }
    public bool IsDone
    {
        get
        {
            if (Monitor.TryEnter(m_Handle))
            {
                bool tmp = m_IsDone;
                Monitor.Exit(m_Handle);
                return tmp;
            }
            return false;
        }
        set
        {
            lock (m_Handle)
            {
                m_IsDone = value;
            }
        }
    }


    public virtual void Start(System.Threading.ThreadPriority priority = ThreadPriority.Normal)
    {
        m_Thread = new System.Threading.Thread(Run);
        m_Thread.Priority = priority;
        m_Thread.Start();
    }
    public virtual void Abort()
    {
        m_Thread.Abort();
    }

    protected virtual void ThreadFunctionCDR() { }

    protected virtual void OnFinished() { }

   
    public virtual bool Update()
    {
        if (IsDone)
        {
            OnFinished();
            return true;
        }
        return false;
    }
    public IEnumerator WaitFor()
    {
        while (!Update())
        {
            yield return null;
        }
    }
    public void Run()
    {
        ThreadFunctionCDR();
        IsDone = true;
    }
}