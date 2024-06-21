from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.side_channel import (
    SideChannel,
    IncomingMessage,
    OutgoingMessage,
)
import numpy as np
import uuid

# Create the StringLogChannel class
class StringLogChannel(SideChannel):

    def __init__(self) -> None:
        super().__init__(uuid.UUID("eb7a9da8-d880-b7ff-7944-c300c6a17e79"))
        self.str = "False,False,False"

    def on_message_received(self, msg: IncomingMessage) -> None:
        """
        Note: We must implement this method of the SideChannel interface to
        receive messages from Unity
        """
        # We simply read a string from the message and print it.
        tmp = msg.read_string()
        if tmp == 'True,False,False' or tmp == 'False,True,False' or tmp == 'False,False,True':
            self.str = tmp
            # print("Class inner:", self.str)

    def send_string(self, data: str) -> None:
        # Add the string to an OutgoingMessage
        msg = OutgoingMessage()
        msg.write_string(data)
        # We call this method to queue the data we want to send
        super().queue_message_to_send(msg)