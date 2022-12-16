using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ConsoleApp4
{
    class Node//класс узлов
    {
        public readonly byte symbol; //символ
        public readonly int freq;//частота
        public readonly Node bit0;//ссылка с битом 0
        public readonly Node bit1;//с битом 1

        public Node(byte symbol, int freq)//констурктор для создания листа
        {
            this.symbol = symbol;
            this.freq = freq;
        }
        public Node(Node bit0, Node bit1, int freq)//конструктор принимает биты и частоту
        {
            this.bit0 = bit0;
            this.bit1 = bit1;
            this.freq = freq;
        }
    }

    class Huffman
    {
        public void CompressFile(string fileName, string ArchiveName) //метод сжатия файла
        {
            byte[] data = File.ReadAllBytes(fileName);//читает файл
            byte[] arch = CompressBytes(data);//вызываем метод компрессии и передаем массив
            File.WriteAllBytes(ArchiveName, arch);//записываем результат
        }

        private byte[] CompressBytes(byte[] data)
        {
            int[] freqs = CalculateFreq(data);//частотный словарь
            Node root = CreateTree(freqs);//дерево
            string[] codes = CreateCodes(root);//массив с кодами (передаем ссылку на корневой эл-т) 
            byte[] head = CreateHeader(data.Length, freqs);//заголовок 
            byte[] bits = Compress(data, codes);//сжатие
            return head.Concat(bits).ToArray();
        }
        private int[] CalculateFreq(byte[] data)//словарь
        {
            int[] freqs = new int[256];
            foreach (byte d in data)//перебираем байты в массиве и считаем
            {
                freqs[d]++;
            }
            NormalizeFreqs();
            return freqs;
            void NormalizeFreqs()//метод нормализации таблиц (чтобы максимальное значение было 255)
            {
                int max = freqs.Max();//максимальный 
                if (max <=255){ return;}//если меньше 255, то все ок
                for (int j = 0; j < 256; j++)//переходим по всем эл и пересчитываем
                {
                    if(freqs[j] > 0)
                        freqs[j] = 1 + freqs[j]*255/(max+1); //эта сумма будет максимально 255 или 254
                }
            }

        }

        private byte[] CreateHeader(int dataLength, int[] freqs)//заголовок
        {
            List<byte> head = new List<byte>();//список заголовка
            head.Add((byte)(dataLength & 255));//в 4 байта записываем 1 бит
            head.Add((byte)((dataLength >> 8) & 255));//сдвигаем вправо на 8
            head.Add((byte)((dataLength >> 16) & 255));//на 16
            head.Add((byte)((dataLength >> 24) & 255));//на 24
            for (int j = 0; j < 256; j++)
            {
                head.Add((byte)freqs[j]);//записываем всю таблицу 
            }
            return head.ToArray();
        }
        private Node CreateTree(int[] freqs)//дерево
        {
            PriorityQueue<Node> pq = new PriorityQueue<Node>();
            for (int j = 0; j < 256; j++)//перебирает все эл-ты в массиве
            {
                if (freqs[j] > 0)
                {
                    pq.Enqueue(freqs[j], new Node((byte)j, freqs[j]));//добавляем новый узел, если больше нуля
                }
            }
            while (pq.Size() > 1)
            {
                Node bit0 = pq.Dequeue();//с нулевым битом эл
                Node bit1 = pq.Dequeue();//с 1 битом эл
                int freq = bit0.freq + bit1.freq;//считаем новую частоту (первая+вторая)
                Node next = new Node(bit0, bit1, freq);//создаем новый узел
                pq.Enqueue(freq, next);//добавялем в очередь 
            }

            return pq.Dequeue();//возвращаем последний эл-т, который остался в очереди
        }
        private string[] CreateCodes(Node root)//метод кодов с рекурсией
        {
            string[] codes = new string[256];//массив на 256 эл
            Next(root, "");//передаем функции откуда начинаем
            return codes;

            void Next(Node node, string code)//функция которая принимаем узел и накапливает код
            {
                if (node.bit0 == null)//если 0-й бит равно нул, мы дошли до листа
                {
                    codes[node.symbol] = code;//записываем код
                }
                else
                {
                    Next(node.bit0, code + "0");//рекурсивно идем по 0-му биту 
                    Next(node.bit1, code + "1");// и по первому
                }
            }

        }

        private byte[] Compress(byte[] data, string[] codes)//сжатие с исходными данными и кодами
        {
            List<byte> bits = new List<byte>();//список байтов 
            byte sum = 0;//переменная для накапливания суммы очередного байта
            byte bit = 1;//тоже накапливает 
            foreach (byte symbol in data)// перебираем все символы
            {
                foreach (char c in codes[symbol])//теперь по символу смотрим код
                {
                    if (c == '1')//если 1 = добавляем бит
                    {
                        sum |= bit;
                    }
                    if (bit < 128)//если меньше 128 = сдвигаем бит
                    {
                        bit <<= 1;
                    }
                    else//если равно 128
                    {
                        bits.Add(sum);//добавляем в список 
                        sum = 0;//обнуляем
                        bit = 1;//записываем первоначальное значение
                    }
                }
            }
            if (bit > 1)
            {
                bits.Add(sum);//еще раз добавляем, если что-то осталось 
            }
            return bits.ToArray();//конвертируем в массив и возвращаем
        }


        public void DecompressFile(string ArchivfileName, string fileName)
        {
            byte[] arch = File.ReadAllBytes(ArchivfileName);//читаем архив
            byte[] data = DecompressBytes(arch);//разжимаем 
            File.WriteAllBytes(fileName, data);//записываем
            Console.WriteLine();
        }
        private byte[] DecompressBytes(byte[] arch)//декомпрессия
        {
            ParseHeader(arch, out int dataLength, out int startIndex, out int[] freqs);//прочитать заголовок
            Node root = CreateTree(freqs);
            byte[] data = Decompress(arch, startIndex, dataLength, root);
            return data;
        }
        private byte[] Decompress(byte[] arch, int startIndex, int dataLength, Node root)
        {
            int size = 0;
            Node curr = root;//текуще положение дерева
            List<byte> data = new List<byte>();
            for (int j = startIndex; j < arch.Length; j++)//начинаем перебирать со стартового индекса и до конца массива
            {
                for (int bit = 1; bit <= 128; bit <<= 1)//перебираем биты
                {
                    bool zero = (arch[j] & bit) == 0;//проверка есть ли 0 в очередном бите
                    if (zero)
                    {
                        curr = curr.bit0;//идем по ветке 0
                    }
                    else { curr = curr.bit1; }//идем по ветке 1

                    if (curr.bit0 != null)//если дальше ссылки нет на 0 или 1 то продолжаем цикл
                    {
                        continue;
                    }
                    if (size++ < dataLength) //увеличиваем размер и сравниваем с длиной, чтобы не выйти за пределы массива
                    { data.Add(curr.symbol); }//если есть место добавляем этот символ, который в листе
                    curr = root; //дойдя до листа, начинаем опять с корня
                }
            }
            return data.ToArray();//конвертируем в массив

        }

        private void ParseHeader(byte[] arch, out int dataLength, out int startIndex, out int[] freqs)
        {
            dataLength = arch[0] |//длинна массива
                (arch[1] << 8) |//сдвигаем на 8
                (arch[1] << 16) |//16
                (arch[1] << 24);//24
            freqs = new int[256];// массив част-го словаря
            for (int j = 0; j < 256; j++)
            {
                freqs[j] = arch[4 + j]; //добавляем 4 потому что 4 байта было потрачено на (вверху арх0, арх1, арх1...)
            }
            startIndex = 4 + 256;
        }
       
    }
    internal class PriorityQueue<T>//приоритетная очередь
    {
        int size;//размер
        SortedDictionary<int, Queue<T>> storage;//отсортированный словарь(список) и библиотека

        public PriorityQueue()//конструктор с значением для размера очереди 0
        {
            storage = new SortedDictionary<int, Queue<T>>();
            size = 0;
        }

        public int Size() => size; //свойство геттер

        public void Enqueue(int priority, T item)//метод для + ел в очередь
        {
            if (!storage.ContainsKey(priority))//если нет ключа, то создаем новую очередь
            {
                storage.Add(priority, new Queue<T>());
            }
            storage[priority].Enqueue(item);//обращаемся к очереди и добавляем эл-т
            size++;//увеличиваем размер
        }

        public T Dequeue()//достаем из очереди ел-т по его приоритету
        {
            if (size == 0)
            {
                throw new System.Exception("Queue is empty");//очередь пустая
            }
            size--;
            foreach (Queue<T> q in storage.Values)//перебираем все эл в приор. очереди
            {
                if (q.Count > 0) //если есть хотя бы 1 эл = возвращаем его
                {
                    return q.Dequeue();
                }
            }
            throw new System.Exception("Queue error");//если дошли сюда = ошибка
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            Huffman huffman = new Huffman();
            huffman.CompressFile("data.txt", "data.txt.huff");
            huffman.DecompressFile("data.txt.huff", "data.txt.huff.txt");

        }
    }
}
