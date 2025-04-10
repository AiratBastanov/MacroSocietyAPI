namespace MacroSocietyAPI.Randoms
{
    public class CreateVerificationCode
    {
        public int RandomInt(int size)
        {
            Random random = new Random();
            int result = 0;
            for (int i = 0; i < size; i++)
            {
                result = (int)((result * 10) + (random.NextDouble() * 9));
                if (size > 1 && result == 0) result++;
            }
            return result;
        }
    }
}
