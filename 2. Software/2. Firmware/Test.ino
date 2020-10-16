

void setup()
{
    Serial.begin(9600);
    pinMode(2, INPUT);
}

void serialEvent(){  //serialEvent
         
}

void loop()
{
    Serial.println("Start");
    delay(25000);
}

