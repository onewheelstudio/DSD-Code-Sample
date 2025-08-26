using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OWS.ObjectPooling
{
    public class ObjectPool<T> : IPool<T> where T : MonoBehaviour, IPoolable<T>
    {
        public ObjectPool(GameObject pooledObject, int numToSpawn = 0)
        {
            this.prefab = pooledObject;
            this.pooledObject = pooledObject.GetComponent<T>();
            Spawn(numToSpawn);
        }

        public ObjectPool(GameObject pooledObject, System.Action<T> pullObject, System.Action<T> pushObject, int numToSpawn = 0)
        {
            this.prefab = pooledObject;
            this.pooledObject = pooledObject.GetComponent<T>();
            this.pullObject = pullObject;
            this.pushObject = pushObject;
        }

        public ObjectPool(T pooledObject, int numToSpawn = 0)
        {
            this.pooledObject = pooledObject;
            this.prefab = pooledObject.gameObject;
            Spawn(numToSpawn);
        }

        private System.Action<T> pullObject;
        private System.Action<T> pushObject;
        private Stack<T> pooledObjects = new Stack<T>();
        private GameObject prefab;
        private T pooledObject;
        public bool ToggleObjects = true;
        public int pooledCount
        {
            get
            {
                if (pooledObjects == null)
                    return 0;

                return pooledObjects.Count;
            }
        }

        public T Pull()
        {
            T t;
            if (pooledCount > 0)
                t = pooledObjects.Pop();
            else if(pooledObject != null)
            {
                t = Object.Instantiate<T>(pooledObject);
                t.Initialize(Push);
            }
            else
            {
                t = GameObject.Instantiate(prefab).GetComponent<T>();
                t.Initialize(Push);
            }
            
            if (t == null)
            {
                return Pull(); //move on to the next object in the pool
            }

            if(ToggleObjects)
                t.gameObject.SetActive(true); //ensure the object is on

            //allow default behavior and turning object back on
            pullObject?.Invoke(t);

            return t;
        }

        public T Pull(Vector3 position)
        {
            T t = Pull();
            t.transform.position = position;
            return t;
        }

        public T Pull(Vector3 position, Quaternion rotation)
        {
            T t = Pull();
            t.transform.SetPositionAndRotation(position, rotation);
            return t;
        }

        public GameObject PullGameObject()
        {
            T t = Pull();

            if (t == null)
                return null;

            return t.gameObject;
        }

        public GameObject PullGameObject(Vector3 position)
        {
            GameObject go = Pull().gameObject;
            go.transform.position = position;
            return go;
        }

        public GameObject PullGameObject(Vector3 position, Quaternion rotation)
        {
            GameObject go = Pull().gameObject;
            go.transform.SetPositionAndRotation(position, rotation);
            return go;
        }

        public void Push(T t)
        {
            pooledObjects.Push(t);

            //create default behavior to turn off objects
            pushObject?.Invoke(t);

            if(ToggleObjects)
                t.gameObject.SetActive(false);
            else
                t.transform.position = Vector3.up * 1000;
        }

        public void AddToPool(int number)
        {
           if (number <= 0)
                return;

            Spawn(number);
        }

        private void Spawn(int number)
        {
            T t;

            for (int i = 0; i < number; i++)
            {
                t = Object.Instantiate<T>(pooledObject);
                if (ToggleObjects)
                    t.gameObject.SetActive(false); 
                else
                    t.transform.position = Vector3.up * 1000;
                
                t.Initialize(Push); //initialize here to avoid double adding to pool - if object is turned off
                pooledObjects.Push(t);
            }
        }
        
        //private async Awaitable SpawnAsync(int number)
        //{
        //    T t;

        //    for (int i = 0; i < number; i++)
        //    {
        //        var spawn = await GameObject.InstantiateAsync(prefab);
        //        spawn. .GetComponent<T>();
        //        pooledObjects.Push(t);
        //        if(ToggleObjects)
        //            t.gameObject.SetActive(false);
        //        else
        //            t.transform.position = Vector3.up * 1000;
        //    }
        //}

        public void DestroyPoolObjects()
        {
            foreach (T item in pooledObjects)
                GameObject.Destroy(item.gameObject);

            pooledObjects.Clear();
        }
    }

    public interface IPool<T>
    {
        T Pull();
        void Push(T t);
    }

    public interface IPoolable<T>
    {
        void Initialize(System.Action<T> returnAction);
        void ReturnToPool();
    }
}
