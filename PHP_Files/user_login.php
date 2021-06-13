
<?php
    session_start();

    //$userID = $_GET["userID"];
    $email = $_GET["email"];
    $pass = $_GET["pass"];
        
    if (!filter_var($email, FILTER_VALIDATE_EMAIL)) {
    echo "Email address '$email' is considered invalid.\n";
    }
    elseif(preg_match("/\\s/", $pass)){
        echo "Password is considered invalid";
        }
    else{
        //Connect
        include"Connect.php";
        
        $query = "SELECT * FROM `KDev_users` WHERE email= '$email'";

        if (!($result = $mysqli->query($query)))
            {
            
                showerror($mysqli->errno, $mysqli->error);
            
            }else{
                $row = $result->fetch_assoc();

                if($pass == $row["password"]){

                    $userID = $row["id"];
                    $_SESSION['user_id'] = $userID;
                    
                    //Get Userinfo from KDev_users $row ID
                    $userDataQuery = "SELECT id, name, email, password FROM `KDev_users` WHERE id= $userID";

                    if (!($userResult = $mysqli->query($userDataQuery)))
                    {
                        //Send Error
                        showerror($mysqli->errno, $mysqli->error);
                        
                    }else
                    {
                        $userRow = $userResult->fetch_assoc();
                        
                        echo json_encode($userRow);
                    } 


                }else{
                    //http_response_code(404);
                    //showerror($mysqli->errno, $mysqli->error);
                }
            }
    }
        
   

    

?>

