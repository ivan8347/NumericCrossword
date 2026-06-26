using System;
using System.Collections.Generic;
using NumericCrossword.Models;

namespace NumericCrossword.Core
{
    /// Генератор кроссворда.
    /// Принимает seed, сложность и шаблон.
    /// Возвращает список формул, готовых к отрисовке.
    public class CrosswordGenerator
    {
        private Random rnd;

        public CrosswordGenerator(int seed)
        {
            // Инициализируем генератор случайных чисел фиксированным seed
            // Это гарантирует одинаковый кроссворд у всех игроков
            rnd = new Random(seed);
        }

        /// Главный метод генерации.
        public List<Formula> Generate(TemplateJson template, string difficulty)
        {
            List<Formula> result = new List<Formula>();

            foreach (var slot in template.slots)
            {
                // Генерируем формулу
                Formula f = GenerateFormula(difficulty);

                // Привязываем формулу к позиции в шаблоне
                f.Row = slot.Row;
                f.Col = slot.Col;
                f.Horizontal = slot.Horizontal;

                result.Add(f);
            }

            return result;
        }

        /// Генерация одной формулы в зависимости от сложности.
        private Formula GenerateFormula(string difficulty)
        {
            int a, b, c;
            char op;

            switch (difficulty)
            {
                case "Лёгкий":
                    op = PickOp("+", "-");
                    break;

                case "Средний":
                    op = PickOp("+", "-", "*");
                    break;

                case "Сложный":
                    op = PickOp("+", "-", "*", "/");
                    break;

                default:
                    op = '+';
                    break;
            }

            // Генерация чисел
            a = rnd.Next(1, 20);
            b = rnd.Next(1, 20);

            // Вычисляем результат
            c = Calculate(a, b, op);

            // Если деление не целое — генерируем заново
            if (op == '/' && (a % b != 0))
                return GenerateFormula(difficulty);

            return new Formula
            {
                A = a,
                B = b,
                C = c,
                Op = op
            };
        }

        /// Выбор случайного оператора.
        private char PickOp(params string[] ops)
        {
            string s = ops[rnd.Next(ops.Length)];
            return s[0];
        }

        /// Вычисление результата формулы.
        private int Calculate(int a, int b, char op)
        {
            switch (op)
            {
                case '+': return a + b;
                case '-': return a - b;
                case '*': return a * b;
                case '/': return b != 0 ? a / b : 0;
                default: return 0;
            }
        }

    }
}
