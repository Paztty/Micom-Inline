
String Response = "";
void setup()
{
    Serial.begin(9600);
    pinMode(2, INPUT);
    Serial.println("$010100*38");
}

void serialEvent(){  //serialEven
    Response = Serial.readStringUntil('\r');
    if(Response == "DONE")
    {
    delay(2000);
        Serial.println("$010100*38");
    }

}

void loop()
{

}

