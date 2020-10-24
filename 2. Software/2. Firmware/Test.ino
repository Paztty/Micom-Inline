
String Response = "";
void setup()
{
    Serial.begin(9600);
    pinMode(2, INPUT);
    Serial.println("@010100*66");
}

void serialEvent(){  //serialEven
    Response = Serial.readString();
    if(Response == "@010211*78")
    {
    delay(5000);
        Serial.println("@010100*66");
    }
    if(Response == "@010200*67")
    {
    delay(5000);
        Serial.println("@010100*66");
    }    
    if(Response == "@010201*68")
    {
    delay(5000);
        Serial.println("@010100*66");
    } 
    if(Response == "@010210*77")
    {
    delay(5000);
        Serial.println("@010100*66");
    } 
}

void loop()
{

}

