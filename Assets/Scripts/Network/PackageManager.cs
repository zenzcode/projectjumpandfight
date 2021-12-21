using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Network
{
    public class PackageManager<T> where T : class
    {
        //Event that is called when we require a package to be transmitted
        public event Action<byte[]> OnRequirePackageTransmit;
        
        //The Speed we want to send our packages with
        private float m_sendSpeed = 0.2f;
        //Time when next Tick happens
        private float m_nextTick;

        //the list of packages that are currently queueing to be sent
        private List<T> m_packages;

        //Received Packages from Server we need to take care of
        private Queue<T> m_receivedPackages;
        
        //Accessor for SendSpeed
        public float SendSpeed
        {
            get 
            {
                //We dont want a sendspeed faster than 0.01f
                if (m_sendSpeed < 0.01f)
                    m_sendSpeed = 0.01f;
                return m_sendSpeed;
            }
            set
            {
                m_sendSpeed = value;
            }
        }

        public List<T> Packages
        {
            get
            {
                if (m_packages == null)
                    m_packages = new List<T>();
                return m_packages;
            }
        }
        
        //We add a package that we want to transmit soon
        public void AddPackage(T package){
            Packages.Add(package);
        }
        
        //When we recceive data we want to read and convert it to packages and add to queue
        public void ReceiveData(byte[] bytes)
        {
            if (m_receivedPackages == null)
                m_receivedPackages = new Queue<T>();

            //we put all packages we read from our bytes into queue to dequeue later
            var packages = ReadBytes(bytes).ToArray();
            foreach (var package in packages)
            {
                m_receivedPackages.Enqueue(package);
            }
        }

        //Convert the Bytes we received to a list of packages
        private List<T> ReadBytes(byte[] bytes)
        {
            var binaryFormatter = new BinaryFormatter();
            using var memoryStream = new MemoryStream();
            memoryStream.Write(bytes, 0, bytes.Length);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return binaryFormatter.Deserialize(memoryStream) as List<T>;
        }

        //Called every Tick to keep sending/receiving going
        public void Tick()
        {
            //add to nextTick to find out when nextTick happens
            m_nextTick += 1 / SendSpeed * Time.fixedDeltaTime;

            if (!(m_nextTick >= 1) || Packages.Count <= 0) return;
            m_nextTick -= 1;

            var packageBytes = CreateBytes();
            Packages.Clear();
            OnRequirePackageTransmit?.Invoke(packageBytes);
        }

        public T GetNextDataReceive()
        {
            if (m_receivedPackages == null || m_receivedPackages.Count == 0) return default(T);
            return m_receivedPackages.Dequeue();
        }

        private byte[] CreateBytes()
        {
            var binaryFormatter = new BinaryFormatter();
            using var memoryStream = new MemoryStream();
            binaryFormatter.Serialize(memoryStream, Packages);
            return memoryStream.ToArray();
        }
        
    }

}
